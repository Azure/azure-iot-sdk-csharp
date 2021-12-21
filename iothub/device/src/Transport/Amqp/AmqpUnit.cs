﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpUnit : IDisposable
    {
        // If the first argument is set to true, we are disconnecting gracefully via CloseAsync.
        private readonly IDeviceIdentity _deviceIdentity;

        private readonly Func<MethodRequestInternal, Task> _onMethodCallback;
        private readonly Action<Twin, string, TwinCollection, IotHubException> _twinMessageListener;
        private readonly Func<string, Message, Task> _onModuleMessageReceivedCallback;
        private readonly Func<Message, Task> _onDeviceMessageReceivedCallback;
        private readonly IAmqpConnectionHolder _amqpConnectionHolder;
        private readonly Action _onUnitDisconnected;
        private volatile bool _disposed;
        private volatile bool _closed;

        private readonly SemaphoreSlim _sessionSemaphore = new SemaphoreSlim(1, 1);

        private AmqpIotSendingLink _messageSendingLink;
        private AmqpIotReceivingLink _messageReceivingLink;
        private readonly SemaphoreSlim _messageReceivingLinkSemaphore = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim _messageReceivingCallbackSemaphore = new SemaphoreSlim(1, 1);
        private bool _isDeviceReceiveMessageCallbackSet;

        private AmqpIotReceivingLink _eventReceivingLink;
        private readonly SemaphoreSlim _eventReceivingLinkSemaphore = new SemaphoreSlim(1, 1);

        private AmqpIotSendingLink _methodSendingLink;
        private AmqpIotReceivingLink _methodReceivingLink;
        private readonly SemaphoreSlim _methodLinkSemaphore = new SemaphoreSlim(1, 1);

        private AmqpIotSendingLink _twinSendingLink;
        private AmqpIotReceivingLink _twinReceivingLink;
        private readonly SemaphoreSlim _twinLinksSemaphore = new SemaphoreSlim(1, 1);

        private AmqpIotSession _amqpIotSession;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;

        public AmqpUnit(
            IDeviceIdentity deviceIdentity,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            _deviceIdentity = deviceIdentity;
            _onMethodCallback = onMethodCallback;
            _twinMessageListener = twinMessageListener;
            _onModuleMessageReceivedCallback = onModuleMessageReceivedCallback;
            _onDeviceMessageReceivedCallback = onDeviceMessageReceivedCallback;
            _amqpConnectionHolder = amqpConnectionHolder;
            _onUnitDisconnected = onUnitDisconnected;

            Logging.Associate(this, _deviceIdentity, nameof(_deviceIdentity));
        }

        internal IDeviceIdentity GetDeviceIdentity()
        {
            return _deviceIdentity;
        }

        #region Open-Close

        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(OpenAsync));

            try
            {
                _closed = false;
                await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, nameof(OpenAsync));
            }
        }

        internal async Task<AmqpIotSession> EnsureSessionIsOpenAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnsureSessionIsOpenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for opening an AMQP session.");
            }

            try
            {
                if (_amqpIotSession == null || _amqpIotSession.IsClosing())
                {
                    _amqpIotSession?.SafeClose();

                    _amqpIotSession = await _amqpConnectionHolder.OpenSessionAsync(_deviceIdentity, cancellationToken).ConfigureAwait(false);
                    Logging.Associate(this, _amqpIotSession, nameof(_amqpIotSession));

                    if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        _amqpAuthenticationRefresher = await _amqpConnectionHolder.CreateRefresherAsync(_deviceIdentity, cancellationToken).ConfigureAwait(false);
                        Logging.Associate(this, _amqpAuthenticationRefresher, nameof(_amqpAuthenticationRefresher));
                    }

                    _amqpIotSession.Closed += OnSessionDisconnected;
                    _messageSendingLink = await _amqpIotSession.OpenTelemetrySenderLinkAsync(_deviceIdentity, cancellationToken).ConfigureAwait(false);
                    _messageSendingLink.Closed += (obj, arg) =>
                    {
                        _amqpIotSession.SafeClose();
                    };

                    Logging.Associate(this, _messageSendingLink, nameof(_messageSendingLink));
                }

                if (_disposed)
                {
                    throw new IotHubException("Device is now offline.", false);
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

            Logging.Exit(this, nameof(EnsureSessionIsOpenAsync));

            return _amqpIotSession;
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(CloseAsync));

            try
            {
                await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for closing an AMQP session.");
            }

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
                _closed = true;
                Logging.Exit(this, nameof(CloseAsync));

                _sessionSemaphore.Release();
            }
        }

        private void Cleanup()
        {
            Logging.Enter(this, nameof(Cleanup));

            _amqpIotSession?.SafeClose();
            _amqpAuthenticationRefresher?.StopLoop();
            if (!_deviceIdentity.IsPooling())
            {
                _amqpConnectionHolder?.Shutdown();
            }

            Logging.Exit(this, nameof(Cleanup));
        }

        #endregion Open-Close

        #region Message

        private async Task EnsureMessageReceivingLinkIsOpenAsync(CancellationToken cancellationToken, bool enableCallback = false)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnsureMessageReceivingLinkIsOpenAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _messageReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP message receiver links are open.");
            }

            try
            {
                if (_messageReceivingLink == null || _messageReceivingLink.IsClosing())
                {
                    _messageReceivingLink?.SafeClose();

                    _messageReceivingLink = await amqpIotSession.OpenMessageReceiverLinkAsync(_deviceIdentity, cancellationToken).ConfigureAwait(false);

                    _messageReceivingLink.Closed += (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };
                    Logging.Associate(this, this, _messageReceivingLink, nameof(EnsureMessageReceivingLinkIsOpenAsync));
                }

                if (enableCallback)
                {
                    _messageReceivingLink.RegisterReceiveMessageListener(OnDeviceMessageReceived);
                }
            }
            finally
            {
                _messageReceivingLinkSemaphore.Release();
                Logging.Exit(this, nameof(EnsureMessageReceivingLinkIsOpenAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendMessagesAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            Logging.Enter(this, messages, nameof(SendMessagesAsync));

            await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, nameof(SendMessagesAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendMessageAsync(Message message, CancellationToken cancellationToken)
        {
            Logging.Enter(this, message, nameof(SendMessageAsync));

            await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, message, nameof(SendMessageAsync));
            }
        }

        public async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (_isDeviceReceiveMessageCallbackSet)
            {
                Logging.Error(this, "Callback handler set for receiving c2d messages, ReceiveAsync() will now always return null", nameof(ReceiveMessageAsync));
                return null;
            }

            Logging.Enter(this, nameof(ReceiveMessageAsync));

            await EnsureMessageReceivingLinkIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageReceivingLink.ReceiveAmqpMessageAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, nameof(ReceiveMessageAsync));
            }
        }

        public async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnableReceiveMessageAsync));

            try
            {
                // Wait to grab the semaphore, and then open the telemetry receiving link and set the callback,
                // and set _isDeviceReceiveMessageCallbackSet  to true.
                // Once _isDeviceReceiveMessageCallbackSet  is set to true, all received c2d messages will be returned on the callback,
                // and not via the polling ReceiveAsync() call.
                try
                {
                    await _messageReceivingCallbackSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException("Failed to enter the semaphore required for ensuring that" +
                        " AMQP message receiver links are open and a listener can be set.");
                }

                await EnsureMessageReceivingLinkIsOpenAsync(cancellationToken, true).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = true;
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();
                Logging.Exit(this, nameof(EnableReceiveMessageAsync));
            }
        }

        public async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(DisableReceiveMessageAsync));

            try
            {
                // Wait to grab the semaphore, and then close the telemetry receiving link and set _isDeviceReceiveMessageCallbackSet  to false.
                // Once _isDeviceReceiveMessageCallbackSet  is set to false, all received c2d messages can be returned via the polling ReceiveAsync() call.
                try
                {
                    await _messageReceivingCallbackSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException("Failed to enter the semaphore required for ensuring that" +
                        " AMQP message receiver links are closed.");
                }

                await DisableMessageReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = false;
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();
                Logging.Exit(this, nameof(DisableReceiveMessageAsync));
            }
        }

        public async Task DisableMessageReceivingLinkAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(DisableMessageReceivingLinkAsync));

            Debug.Assert(_messageReceivingLink != null);

            try
            {
                await _messageReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP message receiver links are closed.");
            }

            try
            {
                await _messageReceivingLink.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _messageReceivingLinkSemaphore.Release();
                Logging.Exit(this, nameof(DisableMessageReceivingLinkAsync));
            }
        }

        private void OnDeviceMessageReceived(Message message)
        {
            Logging.Enter(this, message, nameof(OnDeviceMessageReceived));

            try
            {
                _onDeviceMessageReceivedCallback?.Invoke(message);
            }
            finally
            {
                Logging.Exit(this, message, nameof(OnDeviceMessageReceived));
            }
        }

        public async Task<AmqpIotOutcome> DisposeMessageAsync(string lockToken, AmqpIotDisposeActions disposeAction, CancellationToken cancellationToken)
        {
            Logging.Enter(this, lockToken, nameof(DisposeMessageAsync));

            AmqpIotOutcome disposeOutcome;
            if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
            {
                await EnsureMessageReceivingLinkIsOpenAsync(cancellationToken).ConfigureAwait(false);
                disposeOutcome = await _messageReceivingLink
                    .DisposeMessageAsync(lockToken, AmqpIotResultAdapter.GetResult(disposeAction), cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                disposeOutcome = await _eventReceivingLink
                    .DisposeMessageAsync(lockToken, AmqpIotResultAdapter.GetResult(disposeAction), cancellationToken)
                    .ConfigureAwait(false);
            }
            Logging.Exit(this, lockToken, nameof(DisposeMessageAsync));

            return disposeOutcome;
        }

        #endregion Message

        #region Event

        public async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnableEventReceiveAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _eventReceivingLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP event receiver links are open.");
            }

            try
            {
                if (_eventReceivingLink == null || _eventReceivingLink.IsClosing())
                {
                    _eventReceivingLink?.SafeClose();

                    _eventReceivingLink = await amqpIotSession.OpenEventsReceiverLinkAsync(_deviceIdentity, cancellationToken).ConfigureAwait(false);
                    _eventReceivingLink.Closed += (obj, arg) =>
                    {
                        amqpIotSession.SafeClose();
                    };
                    _eventReceivingLink.RegisterEventListener(OnEventsReceived);
                    Logging.Associate(this, this, _eventReceivingLink, nameof(EnableEventReceiveAsync));
                }
            }
            finally
            {
                _eventReceivingLinkSemaphore.Release();
                Logging.Exit(this, nameof(EnableEventReceiveAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendEventsAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            Logging.Enter(this, messages, nameof(SendEventsAsync));

            try
            {
                return await SendMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, nameof(SendEventsAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            Logging.Enter(this, message, nameof(SendEventAsync));

            try
            {
                return await SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, message, nameof(SendEventAsync));
            }
        }

        public void OnEventsReceived(Message message)
        {
            _onModuleMessageReceivedCallback?.Invoke(message.InputName, message);
        }

        #endregion Event

        #region Method

        public async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnableMethodsAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _methodLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP method sender and receiver links are open.");
            }

            string correlationIdSuffix = Guid.NewGuid().ToString();
            try
            {
                await Task.WhenAll(
                    OpenMethodsReceiverLinkAsync(amqpIotSession, correlationIdSuffix, cancellationToken),
                    OpenMethodsSenderLinkAsync(amqpIotSession, correlationIdSuffix, cancellationToken)
                ).ConfigureAwait(false);
            }
            finally
            {
                _methodLinkSemaphore.Release();
                Logging.Exit(this, nameof(EnableMethodsAsync));
            }
        }

        private async Task OpenMethodsReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_methodReceivingLink == null || _methodReceivingLink.IsClosing())
            {
                _methodReceivingLink?.SafeClose();

                _methodReceivingLink = await amqpIotSession
                    .OpenMethodsReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, cancellationToken)
                    .ConfigureAwait(false);
                _methodReceivingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                _methodReceivingLink.RegisterMethodListener(OnMethodReceived);
                Logging.Associate(this, _methodReceivingLink, nameof(_methodReceivingLink));
            }
        }

        public async Task DisableTwinLinksAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(DisableTwinLinksAsync));

            Debug.Assert(_twinSendingLink != null);
            Debug.Assert(_twinReceivingLink != null);

            try
            {
                await _twinLinksSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP twin sender and receiver links are closed.");
            }

            try
            {
                ICollection<Task> tasks = new List<Task>();
                if (_twinReceivingLink != null)
                {
                    tasks.Add(_twinReceivingLink.CloseAsync(cancellationToken));
                }

                if (_twinSendingLink != null)
                {
                    tasks.Add(_twinSendingLink.CloseAsync(cancellationToken));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _twinReceivingLink = null;
                    _twinSendingLink = null;
                }
            }
            finally
            {
                Logging.Exit(this, nameof(DisableTwinLinksAsync));
                _twinLinksSemaphore.Release();
            }
        }

        public async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(DisableMethodsAsync));

            Debug.Assert(_methodSendingLink != null);
            Debug.Assert(_methodReceivingLink != null);

            try
            {
                await _methodLinkSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP method sender and receiver links are closed.");
            }

            try
            {
                ICollection<Task> tasks = new List<Task>();
                if (_methodReceivingLink != null)
                {
                    tasks.Add(_methodReceivingLink.CloseAsync(cancellationToken));
                }

                if (_methodSendingLink != null)
                {
                    tasks.Add(_methodSendingLink.CloseAsync(cancellationToken));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _methodReceivingLink = null;
                    _methodSendingLink = null;
                }
            }
            finally
            {
                Logging.Exit(this, nameof(DisableMethodsAsync));
                _methodLinkSemaphore.Release();
            }
        }

        private async Task OpenMethodsSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_methodSendingLink == null || _methodSendingLink.IsClosing())
            {
                _methodSendingLink?.SafeClose();

                _methodSendingLink = await amqpIotSession.OpenMethodsSenderLinkAsync(_deviceIdentity, correlationIdSuffix, cancellationToken).ConfigureAwait(false);
                _methodSendingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                Logging.Associate(this, _methodSendingLink, nameof(_methodSendingLink));
            }
        }

        private void OnMethodReceived(MethodRequestInternal methodRequestInternal)
        {
            Logging.Enter(this, methodRequestInternal, nameof(OnMethodReceived));

            try
            {
                _onMethodCallback?.Invoke(methodRequestInternal);
            }
            finally
            {
                Logging.Exit(this, methodRequestInternal, nameof(OnMethodReceived));
            }
        }

        public async Task<AmqpIotOutcome> SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            Logging.Enter(this, methodResponse, nameof(SendMethodResponseAsync));

            await EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(_methodSendingLink != null);

            try
            {
                return await _methodSendingLink.SendMethodResponseAsync(methodResponse, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, methodResponse, nameof(SendMethodResponseAsync));
            }
        }

        #endregion Method

        #region Twin

        internal async Task EnableTwinLinksAsync(CancellationToken cancellationToken)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, nameof(EnableTwinLinksAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _twinLinksSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP twin sender and receiver links are open.");
            }

            try
            {
                string correlationIdSuffix = Guid.NewGuid().ToString();

                await Task
                    .WhenAll(
                       OpenTwinReceiverLinkAsync(amqpIotSession, correlationIdSuffix, cancellationToken),
                       OpenTwinSenderLinkAsync(amqpIotSession, correlationIdSuffix, cancellationToken))
                    .ConfigureAwait(false);
            }
            finally
            {
                _twinLinksSemaphore.Release();
                Logging.Exit(this, nameof(EnableTwinLinksAsync));
            }
        }

        private async Task OpenTwinReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_twinReceivingLink == null || _twinReceivingLink.IsClosing())
            {
                _twinReceivingLink?.SafeClose();

                _twinReceivingLink = await amqpIotSession
                    .OpenTwinReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, cancellationToken)
                    .ConfigureAwait(false);
                _twinReceivingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                _twinReceivingLink.RegisterTwinListener(OnDesiredPropertyReceived);
                Logging.Associate(this, _twinReceivingLink, nameof(_twinReceivingLink));
            }
        }

        private async Task OpenTwinSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, CancellationToken cancellationToken)
        {
            if (_twinSendingLink == null || _twinSendingLink.IsClosing())
            {
                _twinSendingLink?.SafeClose();

                _twinSendingLink = await amqpIotSession
                    .OpenTwinSenderLinkAsync(_deviceIdentity, correlationIdSuffix, cancellationToken)
                    .ConfigureAwait(false);
                _twinSendingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                Logging.Associate(this, _twinSendingLink, nameof(_twinSendingLink));
            }
        }

        private void OnDesiredPropertyReceived(Twin twin, string correlationId, TwinCollection twinCollection, IotHubException ex = default)
        {
            Logging.Enter(this, twin, nameof(OnDesiredPropertyReceived));

            try
            {
                _twinMessageListener?.Invoke(twin, correlationId, twinCollection, ex);
            }
            finally
            {
                Logging.Exit(this, twin, nameof(OnDesiredPropertyReceived));
            }
        }

        public async Task SendTwinMessageAsync(
            AmqpTwinMessageType amqpTwinMessageType,
            string correlationId,
            TwinCollection reportedProperties,
            CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(SendTwinMessageAsync));

            await EnableTwinLinksAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(_twinSendingLink != null);

            try
            {
                AmqpIotOutcome amqpIotOutcome;
                switch (amqpTwinMessageType)
                {
                    case AmqpTwinMessageType.Get:
                        amqpIotOutcome = await _twinSendingLink.SendTwinGetMessageAsync(correlationId, cancellationToken).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;

                    case AmqpTwinMessageType.Patch:
                        amqpIotOutcome = await _twinSendingLink.SendTwinPatchMessageAsync(correlationId, reportedProperties, cancellationToken).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;

                    case AmqpTwinMessageType.Put:
                        amqpIotOutcome = await _twinSendingLink.SubscribeToDesiredPropertiesAsync(correlationId, cancellationToken).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;
                }
            }
            finally
            {
                Logging.Exit(this, nameof(SendTwinMessageAsync));
            }
        }

        #endregion Twin

        #region Connectivity Event

        public void OnConnectionDisconnected()
        {
            Logging.Enter(this, nameof(OnConnectionDisconnected));

            _amqpAuthenticationRefresher?.StopLoop();
            _onUnitDisconnected();

            Logging.Exit(this, nameof(OnConnectionDisconnected));
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            Logging.Enter(this, o, nameof(OnSessionDisconnected));

            if (ReferenceEquals(o, _amqpIotSession))
            {
                _amqpAuthenticationRefresher?.StopLoop();
                _onUnitDisconnected();
            }
            Logging.Exit(this, o, nameof(OnSessionDisconnected));
        }

        #endregion Connectivity Event

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                Logging.Enter(this, disposing, nameof(Dispose));

                Cleanup();
                if (!_deviceIdentity.IsPooling())
                {
                    _amqpConnectionHolder?.Dispose();
                }

                // For device sas authenticated clients the authentication refresher is associated with the AMQP unit itself,
                // so it needs to be explicitly disposed.
                _amqpAuthenticationRefresher?.StopLoop();
                _amqpAuthenticationRefresher?.Dispose();

                _sessionSemaphore?.Dispose();
                _messageReceivingLinkSemaphore?.Dispose();
                _messageReceivingCallbackSemaphore?.Dispose();
                _eventReceivingLinkSemaphore?.Dispose();
                _methodLinkSemaphore?.Dispose();
                _twinLinksSemaphore?.Dispose();

                Logging.Exit(this, disposing, nameof(Dispose));
            }
        }

        #endregion IDisposable
    }
}
