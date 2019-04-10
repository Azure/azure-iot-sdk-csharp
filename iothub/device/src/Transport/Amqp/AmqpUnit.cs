// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnit : AbstractStatusWatcher, IAmqpUnit
    {
        private readonly DeviceIdentity _deviceIdentity;
        private readonly IAmqpSessionHolder _amqpSessionHolder;
        private readonly Func<MethodRequestInternal, Task> _methodHandler;
        private readonly Action<AmqpMessage> _twinMessageListener;
        private readonly Func<string, Message, Task> _eventListener;
        private readonly SemaphoreSlim _messageSendingLinkLock;
        private readonly SemaphoreSlim _messageReceivingLinkLock;
        private readonly SemaphoreSlim _eventReceivingLinkLock;
        private readonly SemaphoreSlim _methodsLinkLock;
        private readonly SemaphoreSlim _twinLinksLock;
        private readonly Action<bool> _onUnitDisconnected;
        private readonly Action<IStatusMonitor> _onUnitDisposed;

        private SendingAmqpLink _messageSendingLink;
        private ReceivingAmqpLink _messageReceivingLink;
        private SendingAmqpLink _methodSendingLink;
        private ReceivingAmqpLink _methodReceivingLink;
        private SendingAmqpLink _twinSendingLink;
        private ReceivingAmqpLink _twinReceivingLink;
        // Note: By design, there is no equivalent Module eventSendingLink.
        private ReceivingAmqpLink _eventReceivingLink;

        public AmqpUnit(
            DeviceIdentity deviceIdentity,
            IAmqpSessionHolder amqpSessionHolder,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener,
            Action<bool> onUnitDisconnected,
            Action<IStatusMonitor> onUnitDisposed) : base()
        {
            _deviceIdentity = deviceIdentity;
            _amqpSessionHolder = amqpSessionHolder;
            _methodHandler = methodHandler;
            _twinMessageListener = twinMessageListener;
            _eventListener = eventListener;
            _onUnitDisconnected = onUnitDisconnected;
            _onUnitDisposed = onUnitDisposed;

            _messageSendingLinkLock = new SemaphoreSlim(1, 1);
            _messageReceivingLinkLock = new SemaphoreSlim(1, 1);
            _eventReceivingLinkLock = new SemaphoreSlim(1, 1);
            _methodsLinkLock = new SemaphoreSlim(1, 1);
            _twinLinksLock = new SemaphoreSlim(1, 1);
            if (Logging.IsEnabled) Logging.Associate(this, _deviceIdentity, $"{nameof(_deviceIdentity)}");
        }
        
        #region Usability
        public bool IsUsable()
        {
            lock (_stateLock)
            {
                return !_disposed;
            }
        }
        #endregion

        #region Open-Close
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenAsync)}");
            ThrowExceptionIfDisposed();

            lock (_stateLock)
            {
                 _closed = false;
            }

            await EnsureMessageSendingLinkAsync(timeout).ConfigureAwait(false);
            
            ChangeStatus(Status.Open);
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenAsync)}");
        }

        private async Task<SendingAmqpLink> EnsureMessageSendingLinkAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureMessageSendingLinkAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _messageSendingLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_messageSendingLink?.IsClosing() ?? true)
                {
                    _messageSendingLink = await AmqpHelper.OpenTelemetrySenderLinkAsync(_amqpSessionHolder, timeout).ConfigureAwait(false);
                }
            }
            finally
            {
                _messageSendingLinkLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureMessageSendingLinkAsync)}");
            return _messageSendingLink;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");
            ThrowExceptionIfDisposed();

            lock (_stateLock)
            {
                if (_closed) return;
                ChangeStatus(Status.Closed);
            }

            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CloseAsync)}");
        }
        #endregion

        #region Message

        private async Task<ReceivingAmqpLink> EnsureMessageReceivingLinkAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureMessageReceivingLinkAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _messageReceivingLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_messageReceivingLink?.IsClosing() ?? true)
                {
                    _messageReceivingLink = await AmqpHelper.OpenTelemetryReceiverLinkAsync(_amqpSessionHolder, timeout).ConfigureAwait(false);
                }

                if (Logging.IsEnabled) Logging.Associate(this, this, _messageReceivingLink, $"{nameof(EnsureMessageReceivingLinkAsync)}");
            }
            finally
            {
                _messageReceivingLinkLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureMessageReceivingLinkAsync)}");
            return _messageReceivingLink;
        }

        public async Task<Outcome> SendMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendMessageAsync)}");
            SendingAmqpLink messageSendingLink = await EnsureMessageSendingLinkAsync(timeout).ConfigureAwait(false);
            Outcome outcome = await AmqpHelper.SendAmqpMessageAsync(messageSendingLink, message, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendMessageAsync)}");
            return outcome;
        }

        public async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(ReceiveMessageAsync)}");

            ReceivingAmqpLink messageReceivingLink = await EnsureMessageReceivingLinkAsync(timeout).ConfigureAwait(false);
            
            AmqpMessage amqpMessage = await AmqpHelper.ReceiveAmqpMessageAsync(messageReceivingLink, timeout).ConfigureAwait(false);
            Message message = null;
            if (amqpMessage != null)
            {
                message = new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            return message;
        }

        public async Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            Outcome disposeOutcome;
            if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
            {
                ReceivingAmqpLink messageReceivingLink = await EnsureMessageReceivingLinkAsync(timeout).ConfigureAwait(false);
                disposeOutcome = await AmqpHelper.DisposeMessageAsync(messageReceivingLink, lockToken, outcome, timeout).ConfigureAwait(false);
            }
            else
            {
                ReceivingAmqpLink eventReceivingLink = await EnsureEventReceivingLinkAsync(timeout).ConfigureAwait(false);
                disposeOutcome = await AmqpHelper.DisposeMessageAsync(eventReceivingLink, lockToken, outcome, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            return disposeOutcome;
        }

        #endregion

        #region Event
        public async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            await EnsureEventReceivingLinkAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
        }

        private async Task<ReceivingAmqpLink> EnsureEventReceivingLinkAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureEventReceivingLinkAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _eventReceivingLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_eventReceivingLink?.IsClosing() ?? true)
                {
                    _eventReceivingLink = await AmqpHelper.OpenEventsReceiverLinkAsync(_amqpSessionHolder, timeout).ConfigureAwait(false);
                    _eventReceivingLink.RegisterMessageListener(OnEventsReceived);
                }

                if (Logging.IsEnabled) Logging.Associate(this, this, _eventReceivingLink, $"{nameof(EnsureMessageReceivingLinkAsync)}");
            }
            finally
            {
                _eventReceivingLinkLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureEventReceivingLinkAsync)}");
            return _eventReceivingLink;
        }

        private void OnEventsReceived(AmqpMessage amqpMessage)
        {
            Message message = new Message(amqpMessage)
            {
                LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
            };

            _eventListener?.Invoke(message.InputName, message);
        }
        #endregion

        #region Method
        public async Task EnableMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableMethodsAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _methodsLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_methodReceivingLink?.IsClosing() ?? true)
                {
                    string correlationIdSuffix = Guid.NewGuid().ToString();
                    Task<ReceivingAmqpLink> receiveLinkCreator = AmqpHelper.OpenMethodsReceiverLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    Task<SendingAmqpLink> sendingLinkCreator = AmqpHelper.OpenMethodsSenderLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);
                    _methodReceivingLink = receiveLinkCreator.Result;
                    _methodSendingLink = sendingLinkCreator.Result;
                    _methodReceivingLink.RegisterMessageListener(OnMethodReceived);
                    if (Logging.IsEnabled) Logging.Associate(this, _methodReceivingLink, $"{nameof(_methodReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Associate(this, _methodSendingLink, $"{nameof(_methodSendingLink)}");
                }
            }
            finally
            {
                _methodsLinkLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableMethodsAsync)}");
        }



        public async Task DisableMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableMethodsAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _methodsLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (!_methodReceivingLink?.IsClosing() ?? true)
                {
                    //string correlationIdSuffix = Guid.NewGuid().ToString();
                    //Task<ReceivingAmqpLink> receiveLinkCreator = AmqpHelper.OpenMethodsReceiverLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    //Task<SendingAmqpLink> sendingLinkCreator = AmqpHelper.OpenMethodsSenderLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    //await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);
                    //_methodReceivingLink = receiveLinkCreator.Result;
                    //_methodSendingLink = sendingLinkCreator.Result;
                    //_methodReceivingLink.RegisterMessageListener(OnMethodReceived);
                    if (Logging.IsEnabled) Logging.Associate(this, _methodReceivingLink, $"{nameof(_methodReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Associate(this, _methodSendingLink, $"{nameof(_methodSendingLink)}");
                }
            }
            finally
            {
                _methodsLinkLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(DisableMethodsAsync)}");
        }

        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
            _methodReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
            _methodHandler.Invoke(methodRequestInternal);
            if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
        }

        public async Task<Outcome> SendMethodResponseAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, $"{nameof(SendMethodResponseAsync)}");
            await EnableMethodsAsync(timeout).ConfigureAwait(false);
            Outcome outcome = await AmqpHelper.SendAmqpMessageAsync(_methodSendingLink, message, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, message, $"{nameof(SendMethodResponseAsync)}");
            return outcome;
        }
        #endregion

        #region Twin
        public async Task EnableTwinPatchAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableTwinPatchAsync)}");
            ThrowExceptionIfClosedOrDisposed();

            bool gain = await _twinLinksLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_twinSendingLink?.IsClosing() ?? true)
                {
                    string correlationIdSuffix = Guid.NewGuid().ToString();
                    Task<ReceivingAmqpLink> receiveLinkCreator = AmqpHelper.OpenTwinReceiverLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    Task<SendingAmqpLink> sendingLinkCreator = AmqpHelper.OpenTwinSenderLinkAsync(_amqpSessionHolder, correlationIdSuffix, timeout);
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                    _twinSendingLink = sendingLinkCreator.Result;
                    _twinReceivingLink = receiveLinkCreator.Result;
                    _twinReceivingLink.RegisterMessageListener(OnDesiredPropertyReceived);
                    if (Logging.IsEnabled) Logging.Associate(this, this, _twinReceivingLink, $"{nameof(EnableTwinPatchAsync)}");
                    if (Logging.IsEnabled) Logging.Associate(this, this, _twinSendingLink, $"{nameof(EnableTwinPatchAsync)}");
                }
            }
            finally
            {
                _twinLinksLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableTwinPatchAsync)}");
            }

            ThrowExceptionIfClosedOrDisposed();
        }

        private void OnDesiredPropertyReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            _twinReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
            _twinMessageListener?.Invoke(amqpMessage);
            if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
        }

        public async Task<Outcome> SendTwinMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            await EnableTwinPatchAsync(timeout).ConfigureAwait(false);
            Outcome outcome = await AmqpHelper.SendAmqpMessageAsync(_twinSendingLink, message, timeout).ConfigureAwait(false);
            return outcome;
        }
        #endregion

        public void OnStatusChange(IStatusReportor statusReportor, Status status)
        {
            if (Logging.IsEnabled) Logging.Enter(this, statusReportor, status, $"{nameof(OnStatusChange)}");

            lock (_stateLock)
            {
                if (status == Status.Closed && !_closed)
                {
                    if (Logging.IsEnabled) Logging.Info(this, $"_onUnitDisconnected called with: {_closed}", $"{nameof(OnStatusChange)}");
                    _onUnitDisconnected(_closed);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, statusReportor, status, $"{nameof(OnStatusChange)}");
        }

        protected override void CleanupResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(CleanupResource)}");
            _amqpSessionHolder.Close();
        }

        protected override void DisposeResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(DisposeResource)}");
            _amqpSessionHolder.Dispose();
            _onUnitDisposed?.Invoke(this);
        }
    }
}
