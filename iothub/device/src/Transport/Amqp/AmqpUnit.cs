// Copyright (c) Microsoft. All rights reserved.
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
        private readonly DeviceIdentity _deviceIdentity;

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
            DeviceIdentity deviceIdentity,
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

        internal DeviceIdentity GetDeviceIdentity()
        {
            return _deviceIdentity;
        }

        #region Open-Close

        public async Task OpenAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(OpenAsync));

            try
            {
                _closed = false;
                await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(OpenAsync));
            }
        }

        internal async Task<AmqpIotSession> EnsureSessionIsOpenAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnsureSessionIsOpenAsync));

            bool enteredSemaphore = await _sessionSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for opening an AMQP session.");
            }

            try
            {
                if (_amqpIotSession == null || _amqpIotSession.IsClosing())
                {
                    _amqpIotSession?.SafeClose();

                    _amqpIotSession = await _amqpConnectionHolder.OpenSessionAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                    Logging.Associate(this, _amqpIotSession, nameof(_amqpIotSession));

                    if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        _amqpAuthenticationRefresher = await _amqpConnectionHolder.CreateRefresherAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                        Logging.Associate(this, _amqpAuthenticationRefresher, nameof(_amqpAuthenticationRefresher));
                    }

                    _amqpIotSession.Closed += OnSessionDisconnected;
                    _messageSendingLink = await _amqpIotSession.OpenTelemetrySenderLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);
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

            Logging.Exit(this, timeout, nameof(EnsureSessionIsOpenAsync));

            return _amqpIotSession;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(CloseAsync));

            bool enteredSemaphore = await _sessionSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for closing an AMQP session.");
            }

            try
            {
                if (_amqpIotSession != null && !_amqpIotSession.IsClosing())
                {
                    try
                    {
                        await _amqpIotSession.CloseAsync(timeout).ConfigureAwait(false);
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
                Logging.Exit(this, timeout, nameof(CloseAsync));

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

        private async Task EnsureMessageReceivingLinkIsOpenAsync(TimeSpan timeout, bool enableCallback = false)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnsureMessageReceivingLinkIsOpenAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            bool enteredSemaphore = await _messageReceivingLinkSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP message receiver links are open.");
            }

            try
            {
                if (_messageReceivingLink == null || _messageReceivingLink.IsClosing())
                {
                    _messageReceivingLink?.SafeClose();

                    _messageReceivingLink = await amqpIotSession.OpenMessageReceiverLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);

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
                Logging.Exit(this, timeout, nameof(EnsureMessageReceivingLinkIsOpenAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendMessagesAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            Logging.Enter(this, messages, timeout, nameof(SendMessagesAsync));

            await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessagesAsync(messages, timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, timeout, nameof(SendMessagesAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendMessageAsync(Message message, TimeSpan timeout)
        {
            Logging.Enter(this, message, timeout, nameof(SendMessageAsync));

            await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessageAsync(message, timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, message, timeout, nameof(SendMessageAsync));
            }
        }

        public async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (_isDeviceReceiveMessageCallbackSet)
            {
                Logging.Error(this, "Callback handler set for receiving c2d messages, ReceiveAsync() will now always return null", nameof(ReceiveMessageAsync));
                return null;
            }

            Logging.Enter(this, timeout, nameof(ReceiveMessageAsync));

            await EnsureMessageReceivingLinkIsOpenAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageReceivingLink.ReceiveAmqpMessageAsync(timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(ReceiveMessageAsync));
            }
        }

        public async Task EnableReceiveMessageAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnableReceiveMessageAsync));

            try
            {
                // Wait to grab the semaphore, and then open the telemetry receiving link and set the callback,
                // and set _isDeviceReceiveMessageCallbackSet  to true.
                // Once _isDeviceReceiveMessageCallbackSet  is set to true, all received c2d messages will be returned on the callback,
                // and not via the polling ReceiveAsync() call.
                bool enteredSemaphore = await _messageReceivingCallbackSemaphore.WaitAsync(timeout).ConfigureAwait(false);
                if (!enteredSemaphore)
                {
                    throw new TimeoutException("Failed to enter the semaphore required for ensuring that" +
                        " AMQP message receiver links are open and a listener can be set.");
                }
                await EnsureMessageReceivingLinkIsOpenAsync(timeout, true).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = true;
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();
                Logging.Exit(this, timeout, nameof(EnableReceiveMessageAsync));
            }
        }

        public async Task DisableReceiveMessageAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(DisableReceiveMessageAsync));

            try
            {
                // Wait to grab the semaphore, and then close the telemetry receiving link and set _isDeviceReceiveMessageCallbackSet  to false.
                // Once _isDeviceReceiveMessageCallbackSet  is set to false, all received c2d messages can be returned via the polling ReceiveAsync() call.
                bool enteredSemaphore = await _messageReceivingCallbackSemaphore.WaitAsync(timeout).ConfigureAwait(false);
                if (!enteredSemaphore)
                {
                    throw new TimeoutException("Failed to enter the semaphore required for ensuring that" +
                        " AMQP message receiver links are closed.");
                }
                await DisableMessageReceivingLinkAsync(timeout).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = false;
            }
            finally
            {
                _messageReceivingCallbackSemaphore.Release();
                Logging.Exit(this, timeout, nameof(DisableReceiveMessageAsync));
            }
        }

        public async Task DisableMessageReceivingLinkAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(DisableMessageReceivingLinkAsync));

            Debug.Assert(_messageReceivingLink != null);

            bool enteredSemaphore = await _messageReceivingLinkSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP message receiver links are closed.");
            }

            try
            {
                await _messageReceivingLink.CloseAsync(timeout).ConfigureAwait(false);
            }
            finally
            {
                _messageReceivingLinkSemaphore.Release();
                Logging.Exit(this, timeout, nameof(DisableMessageReceivingLinkAsync));
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

        public async Task<AmqpIotOutcome> DisposeMessageAsync(string lockToken, AmqpIotDisposeActions disposeAction, TimeSpan timeout)
        {
            Logging.Enter(this, lockToken, nameof(DisposeMessageAsync));

            AmqpIotOutcome disposeOutcome;
            if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
            {
                await EnsureMessageReceivingLinkIsOpenAsync(timeout).ConfigureAwait(false);
                disposeOutcome = await _messageReceivingLink
                    .DisposeMessageAsync(lockToken, AmqpIotResultAdapter.GetResult(disposeAction), timeout)
                    .ConfigureAwait(false);
            }
            else
            {
                await EnableEventReceiveAsync(timeout).ConfigureAwait(false);
                disposeOutcome = await _eventReceivingLink
                    .DisposeMessageAsync(lockToken, AmqpIotResultAdapter.GetResult(disposeAction), timeout)
                    .ConfigureAwait(false);
            }
            Logging.Exit(this, lockToken, nameof(DisposeMessageAsync));

            return disposeOutcome;
        }

        #endregion Message

        #region Event

        public async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnableEventReceiveAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            bool enteredSemaphore = await _eventReceivingLinkSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP event receiver links are open.");
            }

            try
            {
                if (_eventReceivingLink == null || _eventReceivingLink.IsClosing())
                {
                    _eventReceivingLink?.SafeClose();

                    _eventReceivingLink = await amqpIotSession.OpenEventsReceiverLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);
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
                Logging.Exit(this, timeout, nameof(EnableEventReceiveAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendEventsAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            Logging.Enter(this, messages, timeout, nameof(SendEventsAsync));

            try
            {
                return await SendMessagesAsync(messages, timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, timeout, nameof(SendEventsAsync));
            }
        }

        public async Task<AmqpIotOutcome> SendEventAsync(Message message, TimeSpan timeout)
        {
            Logging.Enter(this, message, timeout, nameof(SendEventAsync));

            try
            {
                return await SendMessageAsync(message, timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, message, timeout, nameof(SendEventAsync));
            }
        }

        public void OnEventsReceived(Message message)
        {
            _onModuleMessageReceivedCallback?.Invoke(message.InputName, message);
        }

        #endregion Event

        #region Method

        public async Task EnableMethodsAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnableMethodsAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            bool enteredSemaphore = await _methodLinkSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP method sender and receiver links are open.");
            }

            string correlationIdSuffix = Guid.NewGuid().ToString();
            try
            {
                await Task.WhenAll(
                    OpenMethodsReceiverLinkAsync(amqpIotSession, correlationIdSuffix, timeout),
                    OpenMethodsSenderLinkAsync(amqpIotSession, correlationIdSuffix, timeout)
                ).ConfigureAwait(false);
            }
            finally
            {
                _methodLinkSemaphore.Release();
                Logging.Exit(this, timeout, nameof(EnableMethodsAsync));
            }
        }

        private async Task OpenMethodsReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_methodReceivingLink == null || _methodReceivingLink.IsClosing())
            {
                _methodReceivingLink?.SafeClose();

                _methodReceivingLink = await amqpIotSession
                    .OpenMethodsReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout)
                    .ConfigureAwait(false);
                _methodReceivingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                _methodReceivingLink.RegisterMethodListener(OnMethodReceived);
                Logging.Associate(this, _methodReceivingLink, nameof(_methodReceivingLink));
            }
        }

        public async Task DisableTwinLinksAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(DisableTwinLinksAsync));

            Debug.Assert(_twinSendingLink != null);
            Debug.Assert(_twinReceivingLink != null);

            bool enteredSemaphore = await _twinLinksSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP twin sender and receiver links are closed.");
            }

            try
            {
                ICollection<Task> tasks = new List<Task>();
                if (_twinReceivingLink != null)
                {
                    tasks.Add(_twinReceivingLink.CloseAsync(timeout));
                }

                if (_twinSendingLink != null)
                {
                    tasks.Add(_twinSendingLink.CloseAsync(timeout));
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
                Logging.Exit(this, timeout, nameof(DisableTwinLinksAsync));
                _twinLinksSemaphore.Release();
            }
        }

        public async Task DisableMethodsAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(DisableMethodsAsync));

            Debug.Assert(_methodSendingLink != null);
            Debug.Assert(_methodReceivingLink != null);

            bool enteredSemaphore = await _methodLinkSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP method sender and receiver links are closed.");
            }

            try
            {
                ICollection<Task> tasks = new List<Task>();
                if (_methodReceivingLink != null)
                {
                    tasks.Add(_methodReceivingLink.CloseAsync(timeout));
                }

                if (_methodSendingLink != null)
                {
                    tasks.Add(_methodSendingLink.CloseAsync(timeout));
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
                Logging.Exit(this, timeout, nameof(DisableMethodsAsync));
                _methodLinkSemaphore.Release();
            }
        }

        private async Task OpenMethodsSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_methodSendingLink == null || _methodSendingLink.IsClosing())
            {
                _methodSendingLink?.SafeClose();

                _methodSendingLink = await amqpIotSession.OpenMethodsSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout).ConfigureAwait(false);
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

        public async Task<AmqpIotOutcome> SendMethodResponseAsync(MethodResponseInternal methodResponse, TimeSpan timeout)
        {
            Logging.Enter(this, methodResponse, nameof(SendMethodResponseAsync));

            await EnableMethodsAsync(timeout).ConfigureAwait(false);
            Debug.Assert(_methodSendingLink != null);

            try
            {
                return await _methodSendingLink.SendMethodResponseAsync(methodResponse, timeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, methodResponse, nameof(SendMethodResponseAsync));
            }
        }

        #endregion Method

        #region Twin

        internal async Task EnableTwinLinksAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw new IotHubException("Device is now offline.", false);
            }

            Logging.Enter(this, timeout, nameof(EnableTwinLinksAsync));

            AmqpIotSession amqpIotSession = await EnsureSessionIsOpenAsync(timeout).ConfigureAwait(false);
            bool enteredSemaphore = await _twinLinksSemaphore.WaitAsync(timeout).ConfigureAwait(false);
            if (!enteredSemaphore)
            {
                throw new TimeoutException("Failed to enter the semaphore required for ensuring that AMQP twin sender and receiver links are open.");
            }

            try
            {
                string correlationIdSuffix = Guid.NewGuid().ToString();

                await Task
                    .WhenAll(
                       OpenTwinReceiverLinkAsync(amqpIotSession, correlationIdSuffix, timeout),
                       OpenTwinSenderLinkAsync(amqpIotSession, correlationIdSuffix, timeout))
                    .ConfigureAwait(false);
            }
            finally
            {
                _twinLinksSemaphore.Release();
                Logging.Exit(this, timeout, nameof(EnableTwinLinksAsync));
            }
        }

        private async Task OpenTwinReceiverLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_twinReceivingLink == null || _twinReceivingLink.IsClosing())
            {
                _twinReceivingLink?.SafeClose();

                _twinReceivingLink = await amqpIotSession
                    .OpenTwinReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout)
                    .ConfigureAwait(false);
                _twinReceivingLink.Closed += (obj, arg) =>
                {
                    amqpIotSession.SafeClose();
                };
                _twinReceivingLink.RegisterTwinListener(OnDesiredPropertyReceived);
                Logging.Associate(this, _twinReceivingLink, nameof(_twinReceivingLink));
            }
        }

        private async Task OpenTwinSenderLinkAsync(AmqpIotSession amqpIotSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_twinSendingLink == null || _twinSendingLink.IsClosing())
            {
                _twinSendingLink?.SafeClose();

                _twinSendingLink = await amqpIotSession
                    .OpenTwinSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout)
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

        public async Task SendTwinMessageAsync(AmqpTwinMessageType amqpTwinMessageType, string correlationId, TwinCollection reportedProperties, TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(SendTwinMessageAsync));

            await EnableTwinLinksAsync(timeout).ConfigureAwait(false);
            Debug.Assert(_twinSendingLink != null);

            try
            {
                AmqpIotOutcome amqpIotOutcome;
                switch (amqpTwinMessageType)
                {
                    case AmqpTwinMessageType.Get:
                        amqpIotOutcome = await _twinSendingLink.SendTwinGetMessageAsync(correlationId, timeout).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;

                    case AmqpTwinMessageType.Patch:
                        amqpIotOutcome = await _twinSendingLink.SendTwinPatchMessageAsync(correlationId, reportedProperties, timeout).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;

                    case AmqpTwinMessageType.Put:
                        amqpIotOutcome = await _twinSendingLink.SubscribeToDesiredPropertiesAsync(correlationId, timeout).ConfigureAwait(false);
                        amqpIotOutcome?.ThrowIfNotAccepted();
                        break;
                }
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(SendTwinMessageAsync));
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
