// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpUnit : IDisposable
    {
        // AMQP supports a few different ways to acknowledge a message. Below is the mapping
        // from the IoT hub level concept of Complete/Abandon/Reject to the AMQP level concept
        // of Accepted/Released/Rejected.
        private static readonly IReadOnlyDictionary<MessageAcknowledgement, Outcome> s_ackMap = new Dictionary<MessageAcknowledgement, Outcome>
        {
            { MessageAcknowledgement.Complete, new Accepted() },
            { MessageAcknowledgement.Abandon, new Released() },
            { MessageAcknowledgement.Reject, new Rejected() },
        };

        private readonly IConnectionCredentials _connectionCredentials;
        private readonly AdditionalClientInformation _additionalClientInformation;
        private readonly IotHubClientAmqpSettings _amqpSettings;

        private readonly Func<DirectMethodRequest, Task> _onMethodCallback;
        private readonly Action<AmqpMessage, string, IotHubClientException> _twinMessageListener;
        private readonly Func<IncomingMessage, Task<MessageAcknowledgement>> _onMessageReceivedCallback;
        private readonly IAmqpConnectionHolder _amqpConnectionHolder;
        private readonly Action _onUnitDisconnected;
        private volatile bool _isDisposed;
        private volatile bool _isClosed;

        private readonly SemaphoreSlim _sessionSemaphore = new(1, 1);

        private AmqpIotSendingLink _messageSendingLink;
        private AmqpIotReceivingLink _messageReceivingLink;
        private readonly SemaphoreSlim _messageReceivingLinkSemaphore = new(1, 1);

        private readonly SemaphoreSlim _messageReceivingCallbackSemaphore = new(1, 1);

        private AmqpIotReceivingLink _eventReceivingLink;
        private readonly SemaphoreSlim _eventReceivingLinkSemaphore = new(1, 1);
        private EventHandler _eventReceiverLinkDisconnected;

        private AmqpIotSendingLink _methodSendingLink;
        private AmqpIotReceivingLink _methodReceivingLink;
        private readonly SemaphoreSlim _methodLinkSemaphore = new(1, 1);
        private EventHandler _methodSenderLinkDisconnected;
        private EventHandler _methodReceiverLinkDisconnected;

        private AmqpIotSendingLink _twinSendingLink;
        private AmqpIotReceivingLink _twinReceivingLink;
        private readonly SemaphoreSlim _twinLinksSemaphore = new(1, 1);
        private EventHandler _twinSenderLinkDisconnected;
        private EventHandler _twinReceiverLinkDisconnected;

        private AmqpIotSession _amqpIotSession;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;

        internal AmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<DirectMethodRequest, Task> onMethodCallback,
            Action<AmqpMessage, string, IotHubClientException> twinMessageCallback,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            _connectionCredentials = connectionCredentials;
            _additionalClientInformation = additionalClientInformation;
            _amqpSettings = amqpSettings;

            _onMethodCallback = onMethodCallback;
            _twinMessageListener = twinMessageCallback;
            _onMessageReceivedCallback = onMessageReceivedCallback;

            _amqpConnectionHolder = amqpConnectionHolder;
            _onUnitDisconnected = onUnitDisconnected;

            if (Logging.IsEnabled)
                Logging.Associate(this, _connectionCredentials, nameof(_connectionCredentials));
        }

        // This method returns a tuple of connection credentials and transport settings.
        // This is used only by the AmqpConnectionPool class to resolve the client identity and create a connection holder, if applicable.
        internal (IConnectionCredentials, IotHubClientAmqpSettings) GetConnectionCredentialsAndAmqpSettings()
        {
            return (_connectionCredentials, _amqpSettings);
        }

        #region Open-Close

        internal async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(OpenAsync));

            try
            {
                _isClosed = false;
                await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Under a semaphore, fetch the reference to an AMQP session that is open and active, and has a reference to an opened telemetry sending link.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown if an attempt is made to open a session on a client that is already closed.</exception>
        /// <exception cref="OperationCanceledException"> Thrown if the operation canceled before it could gain access to the semaphore for retrieving the session reference.</exception>
        internal async Task<AmqpIotSession> EnsureSessionIsOpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnsureSessionIsOpenAsync));

            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "Failed to enter the semaphore required for opening an AMQP session.", nameof(EnsureSessionIsOpenAsync));
                throw;
            }

            try
            {
                if (_amqpIotSession == null || _amqpIotSession.IsClosing())
                {
                    // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP session might be in closing state
                    // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                    // library doesn't provide any callbacks for notifying us of the state.
                    // Instead, we have error handling logic when we open sessions.
                    // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                    _amqpIotSession?.SafeClose();

                    _amqpIotSession = await _amqpConnectionHolder.OpenSessionAsync(_connectionCredentials, cancellationToken).ConfigureAwait(false);

                    if (Logging.IsEnabled)
                        Logging.Associate(this, _amqpIotSession, nameof(_amqpIotSession));

                    // In the case of individual SAS authenticated clients, each amqp connection will own its own AMQP token refresh logic
                    if (_connectionCredentials.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        _amqpAuthenticationRefresher = await _amqpConnectionHolder.CreateRefresherAsync(_connectionCredentials, cancellationToken).ConfigureAwait(false);

                        if (Logging.IsEnabled)
                            Logging.Associate(this, _amqpAuthenticationRefresher, nameof(_amqpAuthenticationRefresher));
                    }

                    _amqpIotSession.Closed += OnSessionDisconnected;

                    _messageSendingLink = await _amqpIotSession.OpenTelemetrySenderLinkAsync(
                        _connectionCredentials,
                        _additionalClientInformation,
                        _amqpSettings,
                        cancellationToken).ConfigureAwait(false);

                    _messageSendingLink.Closed += (obj, arg) =>
                    {
                        _amqpIotSession.SafeClose();
                    };

                    if (Logging.IsEnabled)
                        Logging.Associate(this, _messageSendingLink, nameof(_messageSendingLink));
                }

                if (_isDisposed)
                {
                    throw new IotHubClientException("Device is now offline.", false);
                }
            }
            catch (Exception)
            {
                Cleanup();
                throw;
            }
            finally
            {
                _sessionSemaphore.Release();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(EnsureSessionIsOpenAsync));

            return _amqpIotSession;
        }

        internal async Task<DateTime> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(RefreshTokenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _amqpAuthenticationRefresher.RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(RefreshTokenAsync));
            }
        }

        internal DateTime GetRefreshesOn()
        {
            if (_amqpAuthenticationRefresher != null)
            {
                return _amqpAuthenticationRefresher.RefreshOn;
            }
            return DateTime.MaxValue;
        }

        internal async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseAsync));

            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_amqpIotSession != null && !_amqpIotSession.IsClosing())
                {
                    try
                    {
                        await _amqpIotSession.CloseAsync(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        Cleanup();
                    }
                }
            }
            finally
            {
                _isClosed = true;
                _sessionSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CloseAsync));
            }
        }

        private void Cleanup()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(Cleanup));

            if (_amqpIotSession != null)
            {
                _amqpIotSession.Closed -= OnSessionDisconnected;
                _amqpIotSession.SafeClose();
            }
            _amqpAuthenticationRefresher?.StopLoop();

            if (!IsPooled())
            {
                _amqpConnectionHolder?.Shutdown();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(Cleanup));
        }

        private bool IsPooled()
        {
            return _connectionCredentials.ClientCertificate == null
                && _amqpSettings.ConnectionPoolSettings != null
                && _amqpSettings.ConnectionPoolSettings.UsePooling;
        }

        #endregion Open-Close

        #region Message

        internal async Task<AmqpIotOutcome> SendMessagesAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, nameof(SendMessagesAsync));

            await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, nameof(SendMessagesAsync));
            }
        }

        internal async Task<AmqpIotOutcome> SendMessageAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(SendMessageAsync));

            await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, nameof(SendMessageAsync));
            }
        }

        internal async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnableReceiveMessageAsync));

            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            // Wait to grab the semaphore, and then open the telemetry receiving link and set the callback,
            // and set _isDeviceReceiveMessageCallbackSet  to true.
            // Once _isDeviceReceiveMessageCallbackSet  is set to true, all received c2d messages will be returned on the callback,
            // and not via the polling ReceiveAsync() call.
            await _messageReceivingCallbackSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await EnsureMessageReceivingLinkIsOpenAsync(true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(EnableReceiveMessageAsync));
            }
        }

        internal async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DisableReceiveMessageAsync));

            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            await _messageReceivingCallbackSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await DisableMessageReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DisableReceiveMessageAsync));
            }
        }

        internal async Task DisableMessageReceivingLinkAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DisableMessageReceivingLinkAsync));

            Debug.Assert(_messageReceivingLink != null);

            await _messageReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // This event handler is in place for network drop cases and will try to close the session that this
            // link belongs to, but that isn't necessary when the client is deliberately closing just the link.
            if (_eventReceiverLinkDisconnected != null)
            {
                _messageReceivingLink.Closed -= _eventReceiverLinkDisconnected;
                if (_eventReceivingLink != null)
                {
                    _eventReceivingLink.Closed -= _eventReceiverLinkDisconnected;
                }
            }

            try
            {
                await _messageReceivingLink.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _messageReceivingLinkSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DisableMessageReceivingLinkAsync));
            }
        }

        internal async Task DisposeMessageAsync(ArraySegment<byte> deliveryTag, MessageAcknowledgement acknowledgementType, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, deliveryTag, nameof(DisposeMessageAsync));

            if (string.IsNullOrWhiteSpace(_connectionCredentials.ModuleId)
                || string.IsNullOrWhiteSpace(_connectionCredentials.GatewayHostName))
            {
                await EnsureMessageReceivingLinkIsOpenAsync(false, cancellationToken).ConfigureAwait(false);

                // Acknowledge the message over the cloud to device/module message receiver link.
                await _messageReceivingLink
                    .DisposeMessageAsync(deliveryTag, s_ackMap[acknowledgementType], cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await EnableEdgeModuleEventReceiveAsync(cancellationToken).ConfigureAwait(false);

                // Acknowledge the message over the edge hub to module message receiver link.
                await _eventReceivingLink
                    .DisposeMessageAsync(deliveryTag, s_ackMap[acknowledgementType], cancellationToken)
                    .ConfigureAwait(false);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, deliveryTag, nameof(DisposeMessageAsync));
        }

        private async Task EnsureMessageReceivingLinkIsOpenAsync(bool enableCallback, CancellationToken cancellationToken)
        {
            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnsureMessageReceivingLinkIsOpenAsync));

            _amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            await _messageReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_messageReceivingLink == null || _messageReceivingLink.IsClosing())
                {
                    // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                    // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                    // library doesn't provide any callbacks for notifying us of the state.
                    // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                    // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                    // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                    // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                    // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                    _messageReceivingLink?.SafeClose();

                    _messageReceivingLink = await _amqpIotSession
                        .OpenMessageReceiverLinkAsync(
                            _connectionCredentials,
                            _additionalClientInformation,
                            _amqpSettings,
                            cancellationToken)
                        .ConfigureAwait(false);

                    _eventReceiverLinkDisconnected ??= (obj, arg) =>
                        {
                            _amqpIotSession.SafeClose();
                        };

                    _messageReceivingLink.Closed += _eventReceiverLinkDisconnected;

                    if (Logging.IsEnabled)
                        Logging.Associate(this, this, _messageReceivingLink, nameof(EnsureMessageReceivingLinkIsOpenAsync));
                }

                if (enableCallback)
                {
                    _messageReceivingLink.RegisterReceiveMessageListener(OnMessageReceivedAsync);
                }
            }
            finally
            {
                _messageReceivingLinkSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(EnsureMessageReceivingLinkIsOpenAsync));
            }
        }

        private async Task OnMessageReceivedAsync(IncomingMessage message, ArraySegment<byte> deliveryTag)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(OnMessageReceivedAsync));

            try
            {
                if (_onMessageReceivedCallback != null)
                {
                    MessageAcknowledgement acknowledgementType = await _onMessageReceivedCallback.Invoke(message).ConfigureAwait(false);

                    await DisposeMessageAsync(deliveryTag, acknowledgementType, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, nameof(OnMessageReceivedAsync));
            }
        }

        #endregion Message

        #region Event

        internal async Task EnableEdgeModuleEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnableEdgeModuleEventReceiveAsync));

            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            _amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            await _eventReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_eventReceivingLink == null || _eventReceivingLink.IsClosing())
                {
                    // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                    // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                    // library doesn't provide any callbacks for notifying us of the state.
                    // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                    // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                    // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                    // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                    // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                    _eventReceivingLink?.SafeClose();

                    _eventReceivingLink = await _amqpIotSession.OpenEventsReceiverLinkAsync(
                        _connectionCredentials,
                        _additionalClientInformation,
                        _amqpSettings,
                        cancellationToken).ConfigureAwait(false);

                    _eventReceiverLinkDisconnected ??= (obj, arg) =>
                        {
                            _amqpIotSession.SafeClose();
                        };

                    _eventReceivingLink.Closed += _eventReceiverLinkDisconnected;
                    _eventReceivingLink.RegisterEventListener(OnMessageReceivedAsync);

                    if (Logging.IsEnabled)
                        Logging.Associate(this, this, _eventReceivingLink, nameof(EnableEdgeModuleEventReceiveAsync));
                }
            }
            finally
            {
                _eventReceivingLinkSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(EnableEdgeModuleEventReceiveAsync));
            }
        }

        internal async Task<AmqpIotOutcome> SendEventsAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, nameof(SendEventsAsync));

            try
            {
                return await SendMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, nameof(SendEventsAsync));
            }
        }

        internal async Task<AmqpIotOutcome> SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(SendTelemetryAsync));

            try
            {
                return await SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, nameof(SendTelemetryAsync));
            }
        }

        #endregion Event

        #region Method

        internal async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnableMethodsAsync));

            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            _amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            await _methodLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string correlationIdSuffix = Guid.NewGuid().ToString();
                await Task
                    .WhenAll(
                        OpenMethodsReceiverLinkAsync(_amqpIotSession, correlationIdSuffix, cancellationToken),
                        OpenMethodsSenderLinkAsync(_amqpIotSession, correlationIdSuffix, cancellationToken))
                    .ConfigureAwait(false);
            }
            finally
            {
                _methodLinkSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(EnableMethodsAsync));
            }
        }

        internal async Task DisableTwinLinksAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DisableTwinLinksAsync));

            Debug.Assert(_twinSendingLink != null);
            Debug.Assert(_twinReceivingLink != null);

            await _twinLinksSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // These event handlers are in place for network drop cases and will try to close the session that this
            // link belongs to, but that isn't necessary when the client is deliberately closing just the link.
            if (_twinReceiverLinkDisconnected != null)
            {
                _twinReceivingLink.Closed -= _twinReceiverLinkDisconnected;
            }

            if (_twinSenderLinkDisconnected != null)
            {
                _twinSendingLink.Closed -= _twinSenderLinkDisconnected;
            }

            try
            {
                ICollection<Task> tasks = new List<Task>(2);
                if (_twinReceivingLink != null)
                {
                    tasks.Add(_twinReceivingLink.CloseAsync(cancellationToken));
                }

                if (_twinSendingLink != null)
                {
                    tasks.Add(_twinSendingLink.CloseAsync(cancellationToken));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _twinReceivingLink = null;
                    _twinSendingLink = null;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DisableTwinLinksAsync));
                _twinLinksSemaphore.Release();
            }
        }

        internal async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DisableMethodsAsync));

            Debug.Assert(_methodSendingLink != null);
            Debug.Assert(_methodReceivingLink != null);

            await _methodLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // These event handlers are in place for network drop cases and will try to close the session that this
            // link belongs to, but that isn't necessary when the client is deliberately closing just the link.
            if (_methodReceiverLinkDisconnected != null)
            {
                _methodReceivingLink.Closed -= _methodReceiverLinkDisconnected;
            }

            if (_methodSenderLinkDisconnected != null)
            {
                _methodSendingLink.Closed -= _methodSenderLinkDisconnected;
            }

            try
            {
                ICollection<Task> tasks = new List<Task>(2);
                if (_methodReceivingLink != null)
                {
                    tasks.Add(_methodReceivingLink.CloseAsync(cancellationToken));
                }

                if (_methodSendingLink != null)
                {
                    tasks.Add(_methodSendingLink.CloseAsync(cancellationToken));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _methodReceivingLink = null;
                    _methodSendingLink = null;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DisableMethodsAsync));
                _methodLinkSemaphore.Release();
            }
        }

        internal async Task<AmqpIotOutcome> SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodResponse, nameof(SendMethodResponseAsync));

            await EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(_methodSendingLink != null);

            try
            {
                return await _methodSendingLink.SendMethodResponseAsync(methodResponse, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, methodResponse, nameof(SendMethodResponseAsync));
            }
        }

        private async Task OpenMethodsSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_methodSendingLink == null || _methodSendingLink.IsClosing())
            {
                // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                // library doesn't provide any callbacks for notifying us of the state.
                // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                _methodSendingLink?.SafeClose();

                _methodSendingLink = await amqpIotSession.OpenMethodsSenderLinkAsync(
                    _connectionCredentials,
                    _additionalClientInformation,
                    _amqpSettings,
                    correlationIdSuffix,
                    cancellationToken).ConfigureAwait(false);

                if (_methodSenderLinkDisconnected == null)
                {
                    _methodSenderLinkDisconnected = (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };
                }

                _methodSendingLink.Closed += _methodSenderLinkDisconnected;

                if (Logging.IsEnabled)
                    Logging.Associate(this, _methodSendingLink, nameof(_methodSendingLink));
            }
        }

        private void OnMethodReceived(DirectMethodRequest directMethodRequest)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, directMethodRequest, nameof(OnMethodReceived));

            try
            {
                _onMethodCallback?.Invoke(directMethodRequest);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, directMethodRequest, nameof(OnMethodReceived));
            }
        }

        private async Task OpenMethodsReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_methodReceivingLink == null || _methodReceivingLink.IsClosing())
            {
                // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                // library doesn't provide any callbacks for notifying us of the state.
                // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                _methodReceivingLink?.SafeClose();

                _methodReceivingLink = await amqpIotSession
                    .OpenMethodsReceiverLinkAsync(
                        _connectionCredentials,
                        _additionalClientInformation,
                        _amqpSettings,
                        correlationIdSuffix,
                        cancellationToken)
                    .ConfigureAwait(false);

                _methodReceiverLinkDisconnected ??= (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };

                _methodReceivingLink.Closed += _methodReceiverLinkDisconnected;

                _methodReceivingLink.RegisterMethodListener(OnMethodReceived);

                if (Logging.IsEnabled)
                    Logging.Associate(this, _methodReceivingLink, nameof(_methodReceivingLink));
            }
        }

        #endregion Method

        #region Twin

        internal async Task EnableTwinLinksAsync(CancellationToken cancellationToken)
        {
            if (_isClosed)
            {
                throw new IotHubClientException("Device is now offline.", false);
            }

            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnableTwinLinksAsync));

            _amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            await _twinLinksSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                string correlationIdSuffix = Guid.NewGuid().ToString();

                await Task
                    .WhenAll(
                       OpenTwinReceiverLinkAsync(_amqpIotSession, correlationIdSuffix, cancellationToken),
                       OpenTwinSenderLinkAsync(_amqpIotSession, correlationIdSuffix, cancellationToken))
                    .ConfigureAwait(false);
            }
            finally
            {
                _twinLinksSemaphore.Release();
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(EnableTwinLinksAsync));
            }
        }

        internal async Task SendTwinMessageAsync(
            AmqpTwinMessageType amqpTwinMessageType,
            string correlationId,
            ReportedProperties reportedProperties,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendTwinMessageAsync));

            await EnableTwinLinksAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(_twinSendingLink != null);

            try
            {
                AmqpIotOutcome amqpIotOutcome = null;
                switch (amqpTwinMessageType)
                {
                    case AmqpTwinMessageType.Get:
                        amqpIotOutcome = await _twinSendingLink
                            .SendTwinGetMessageAsync(correlationId, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case AmqpTwinMessageType.Patch:
                        amqpIotOutcome = await _twinSendingLink
                            .SendTwinPatchMessageAsync(correlationId, reportedProperties, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case AmqpTwinMessageType.Put:
                        amqpIotOutcome = await _twinSendingLink
                            .SubscribeToDesiredPropertiesAsync(correlationId, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                }

                amqpIotOutcome?.ThrowIfNotAccepted();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(SendTwinMessageAsync));
            }
        }

        private async Task OpenTwinReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_twinReceivingLink == null || _twinReceivingLink.IsClosing())
            {
                // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                // library doesn't provide any callbacks for notifying us of the state.
                // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                _twinReceivingLink?.SafeClose();

                _twinReceivingLink = await amqpIotSession.OpenTwinReceiverLinkAsync(
                    _connectionCredentials,
                    _additionalClientInformation,
                    _amqpSettings,
                    correlationIdSuffix,
                    cancellationToken).ConfigureAwait(false);

                if (_twinReceiverLinkDisconnected == null)
                {
                    _twinReceiverLinkDisconnected = (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };
                }

                _twinReceivingLink.Closed += _twinReceiverLinkDisconnected;

                _twinReceivingLink.RegisterTwinListener(OnDesiredPropertyReceived);

                if (Logging.IsEnabled)
                    Logging.Associate(this, _twinReceivingLink, nameof(_twinReceivingLink));
            }
        }

        private async Task OpenTwinSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_twinSendingLink == null || _twinSendingLink.IsClosing())
            {
                // SafeClose is a fire-and-forget operation. As a result, when it returns the AMQP link might be in closing state
                // and may still be referenced by its parent object. Adding locks or checks for this isn't possible because the AMQP
                // library doesn't provide any callbacks for notifying us of the state.
                // Instead, we have error handling logic when we open links or try to perform operations on opened links.
                // If the operation throws an exception, the error handling code will determine if it is to be tried, and it will retry, if necessary.
                // This call to SafeClose is necassry because the AMQP library does not call SafeClose immediately after a link closure was requested (as a part of its link lifecycle)
                // but instead calls SafeClose eventually. Opening a link with the same name as one previously opened will give rise to ResourceLocked conflicts.
                // Another way to avoid ResourceLocked conflicts is to open links with unique names. This approach was previously adopted but later modified in an attempt to reduce link name length.
                _twinSendingLink?.SafeClose();

                _twinSendingLink = await amqpIotSession.OpenTwinSenderLinkAsync(
                    _connectionCredentials,
                    _additionalClientInformation,
                    _amqpSettings,
                    correlationIdSuffix,
                    cancellationToken).ConfigureAwait(false);

                if (_twinSenderLinkDisconnected == null)
                {
                    _twinSenderLinkDisconnected = (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };
                }

                _twinSendingLink.Closed += _twinSenderLinkDisconnected;

                if (Logging.IsEnabled)
                    Logging.Associate(this, _twinSendingLink, nameof(_twinSendingLink));
            }
        }

        private void OnDesiredPropertyReceived(AmqpMessage responseFromService, string correlationId, IotHubClientException ex = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, responseFromService, nameof(OnDesiredPropertyReceived));

            try
            {
                _twinMessageListener?.Invoke(responseFromService, correlationId, ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, responseFromService, nameof(OnDesiredPropertyReceived));
            }
        }

        #endregion Twin

        #region Connectivity Event

        internal void OnConnectionDisconnected()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(OnConnectionDisconnected));

            _amqpAuthenticationRefresher?.StopLoop();
            _onUnitDisconnected();

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(OnConnectionDisconnected));
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, o, nameof(OnSessionDisconnected));

            if (ReferenceEquals(o, _amqpIotSession))
            {
                _amqpAuthenticationRefresher?.StopLoop();

                // calls TransportHandler.OnTransportDisconnected() which sets the transport layer up to retry
                _onUnitDisconnected();
            }
            if (Logging.IsEnabled)
                Logging.Exit(this, o, nameof(OnSessionDisconnected));
        }

        #endregion Connectivity Event

        #region IDisposable

        public void Dispose()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(
                        this,
                        $"Device pooling={IsPooled()}; disposed={_isDisposed}",
                        $"{nameof(AmqpUnit)}.{nameof(Dispose)}");

                if (!_isDisposed)
                {
                    Cleanup();
                    if (!IsPooled())
                    {
                        _amqpConnectionHolder?.Dispose();
                    }

                    // For device SAS authenticated clients the authentication refresher is associated with the AMQP unit itself,
                    // so it needs to be explicitly stopped.
                    _amqpAuthenticationRefresher?.StopLoop();

                    _sessionSemaphore?.Dispose();
                    _messageReceivingLinkSemaphore?.Dispose();
                    _messageReceivingCallbackSemaphore?.Dispose();
                    _eventReceivingLinkSemaphore?.Dispose();
                    _methodLinkSemaphore?.Dispose();
                    _twinLinksSemaphore?.Dispose();

                    if (Logging.IsEnabled)
                        Logging.Exit(this, nameof(Dispose));
                }

                _isDisposed = true;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(
                        this,
                        $"Device pooling={IsPooled()}; disposed={_isDisposed}",
                        $"{nameof(AmqpUnit)}.{nameof(Dispose)}");
            }
        }

        #endregion IDisposable
    }
}
