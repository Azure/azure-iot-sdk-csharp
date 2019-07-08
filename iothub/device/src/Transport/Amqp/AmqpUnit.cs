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

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpUnit : IDisposable
    {
        private static readonly IotHubException DEVICE_DISCONNECTED_EXCEPTION = new IotHubException("AmqpUnit is disconnected.", false);
        // If the first argument is set to true, we are disconnecting gracefully via CloseAsync.
        private readonly DeviceIdentity _deviceIdentity;
        private readonly Func<MethodRequestInternal, Task> _methodHandler;
        private readonly Action<Twin, string, TwinCollection> _twinMessageListener;
        private readonly Func<string, Message, Task> _eventListener;
        private readonly IAmqpConnectionHolder _amqpConnectionHolder;
        private readonly Action _onUnitDisconnected;
        private volatile bool _disposed;
        private volatile bool _closed;

        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);

        private AmqpIoTSendingLink _messageSendingLink;
        private AmqpIoTReceivingLink _messageReceivingLink;
        private readonly SemaphoreSlim _messageReceivingLinkLock = new SemaphoreSlim(1, 1);

        private AmqpIoTSendingLink _methodSendingLink;
        private AmqpIoTReceivingLink _methodReceivingLink;
        private readonly SemaphoreSlim _methodLinkLock = new SemaphoreSlim(1, 1);

        private AmqpIoTSendingLink _twinSendingLink;
        private AmqpIoTReceivingLink _twinReceivingLink;
        private readonly SemaphoreSlim _twinLinksLock = new SemaphoreSlim(1, 1);

        private AmqpIoTSession _amqpIoTSession;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;

        public AmqpUnit(
            DeviceIdentity deviceIdentity,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<Twin, string, TwinCollection> twinMessageListener, 
            Func<string, Message, Task> eventListener,
            Action onUnitDisconnected)
        {
            _deviceIdentity = deviceIdentity;
            _methodHandler = methodHandler;
            _twinMessageListener = twinMessageListener;
            _eventListener = eventListener;
            _amqpConnectionHolder = amqpConnectionHolder;
            _onUnitDisconnected = onUnitDisconnected;

            if (Logging.IsEnabled) Logging.Associate(this, _deviceIdentity, $"{nameof(_deviceIdentity)}");
        }

        internal DeviceIdentity GetDeviceIdentity()
        {
            return _deviceIdentity;
        }

        #region Open-Close
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenAsync)}");

            try
            {
                _closed = false;
                await EnsureSessionAsync(timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenAsync)}");
            }
        }

        internal async Task<AmqpIoTSession> EnsureSessionAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw DEVICE_DISCONNECTED_EXCEPTION;
            }

            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureSessionAsync)}");
            bool gain = await _sessionLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            Debug.Assert(!_disposed);
            try
            { 
                if (_amqpIoTSession == null || _amqpIoTSession.IsClosing())
                {
                    _amqpIoTSession = await _amqpConnectionHolder.OpenSessionAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                    if (Logging.IsEnabled) Logging.Associate(this, _amqpIoTSession, $"{nameof(_amqpIoTSession)}");
                    if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        _amqpAuthenticationRefresher = await _amqpConnectionHolder.CreateRefresher(_deviceIdentity, timeout).ConfigureAwait(false);
                        if (Logging.IsEnabled) Logging.Associate(this, _amqpAuthenticationRefresher, $"{nameof(_amqpAuthenticationRefresher)}");
                    }

                    _amqpIoTSession.Closed += OnSessionDisconnected;
                    _messageSendingLink = await _amqpIoTSession.OpenTelemetrySenderLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                    _messageSendingLink.Closed += (obj, arg) => {
                        _amqpIoTSession.SafeClose();
                    };

                    if (Logging.IsEnabled) Logging.Associate(this, _messageSendingLink, $"{nameof(_messageSendingLink)}");
                }
                return _amqpIoTSession;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureSessionAsync)}");
                _sessionLock.Release();
            }
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");
            
            bool gain = await _sessionLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_amqpIoTSession != null && !_amqpIoTSession.IsClosing())
                {
                    try
                    {
                        await _amqpIoTSession.CloseAsync(timeout).ConfigureAwait(false);
                    }
                    finally
                    {
                        _amqpAuthenticationRefresher?.StopLoop();
                        _amqpIoTSession.SafeClose();
                        if (!_deviceIdentity.IsPooling())
                        {
                            _amqpConnectionHolder.Dispose();
                        }
                    }
                }
            }
            finally
            {
                _closed = true;
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CloseAsync)}");
                _sessionLock.Release();
            }

        }
        #endregion

        #region Message

        private async Task EnsureReceivingLinkAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw DEVICE_DISCONNECTED_EXCEPTION;
            }

            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureReceivingLinkAsync)}");
            AmqpIoTSession amqpIoTSession = await EnsureSessionAsync(timeout).ConfigureAwait(false);
            bool gain = await _messageReceivingLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_messageReceivingLink == null || _messageReceivingLink.IsClosing())
                {
                    if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
                    {
                        _messageReceivingLink = await amqpIoTSession.OpenTelemetryReceiverLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                    }
                    else
                    {
                        _messageReceivingLink = await amqpIoTSession.OpenEventsReceiverLinkAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                    }

                    _messageReceivingLink.Closed += (obj, arg) => {
                        amqpIoTSession.SafeClose();
                    };
                    if (Logging.IsEnabled) Logging.Associate(this, this, _messageReceivingLink, $"{nameof(EnsureReceivingLinkAsync)}");
                }
            }
            finally
            {
                _messageReceivingLinkLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureReceivingLinkAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendMessagesAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, messages, timeout, $"{nameof(SendMessagesAsync)}");
            await EnsureSessionAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessagesAsync(messages, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, timeout, $"{nameof(SendMessagesAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendMessageAsync(Message message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendMessageAsync)}");
            await EnsureSessionAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageSendingLink.SendMessageAsync(message, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendMessageAsync)}");
            }
        }

        public async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            await EnsureReceivingLinkAsync(timeout).ConfigureAwait(false);
            try
            {
                Debug.Assert(_messageSendingLink != null);
                return await _messageReceivingLink.ReceiveAmqpMessageAsync(timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> DisposeMessageAsync(string lockToken, AmqpIoTDisposeActions disposeAction, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            await EnsureReceivingLinkAsync(timeout).ConfigureAwait(false);
            AmqpIoTOutcome disposeOutcome;
            disposeOutcome = await _messageReceivingLink.DisposeMessageAsync(lockToken, AmqpIoTResultAdapter.GetResult(disposeAction), timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            return disposeOutcome;
        }

        #endregion

        #region Event
        public async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            try
            {
                await EnsureReceivingLinkAsync(timeout).ConfigureAwait(false);
                _messageReceivingLink.RegisterEventListener(OnEventsReceived);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendEventsAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, messages, timeout, $"{nameof(SendEventsAsync)}");
            try
            {
                return await SendMessagesAsync(messages, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, timeout, $"{nameof(SendEventsAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendEventAsync(Message message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendEventAsync)}");
            try
            {
                return await SendMessageAsync(message, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendEventAsync)}");
            }
        }

        public void OnEventsReceived(Message message)
        {
            _eventListener?.Invoke(message.InputName, message);
        }
        #endregion

        #region Method
        public async Task EnableMethodsAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw DEVICE_DISCONNECTED_EXCEPTION;
            }

            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableMethodsAsync)}");
            AmqpIoTSession amqpIoTSession = await EnsureSessionAsync(timeout).ConfigureAwait(false);
            bool gain = await _methodLinkLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }
            
            string correlationIdSuffix = Guid.NewGuid().ToString();
            try
            {
                await Task.WhenAll(
                    OpenMethodsReceiverLinkAsync(amqpIoTSession, correlationIdSuffix, timeout),
                    OpenMethodsSenderLinkAsync(amqpIoTSession, correlationIdSuffix, timeout)
                ).ConfigureAwait(false);
            }
            finally
            {
                _methodLinkLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableMethodsAsync)}");
            }
        }
        
        private async Task OpenMethodsReceiverLinkAsync(AmqpIoTSession amqpIoTSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_methodReceivingLink == null || _methodReceivingLink.IsClosing())
            {
                _methodReceivingLink = await amqpIoTSession.OpenMethodsReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout).ConfigureAwait(false);
                _methodReceivingLink.Closed += (obj, arg) => {
                    amqpIoTSession.SafeClose();
                };
                _methodReceivingLink.RegisterMethodListener(OnMethodReceived);
                if (Logging.IsEnabled) Logging.Associate(this, _methodReceivingLink, $"{nameof(_methodReceivingLink)}");
            }
        }
        private async Task OpenMethodsSenderLinkAsync(AmqpIoTSession amqpIoTSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_methodSendingLink == null || _methodSendingLink.IsClosing())
            {
                _methodSendingLink = await amqpIoTSession.OpenMethodsSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout).ConfigureAwait(false);
                _methodSendingLink.Closed += (obj, arg) => {
                    amqpIoTSession.SafeClose();
                };
                if (Logging.IsEnabled) Logging.Associate(this, _methodSendingLink, $"{nameof(_methodSendingLink)}");
            }
        }

        private void OnMethodReceived(MethodRequestInternal methodRequestInternal)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodRequestInternal, $"{nameof(OnMethodReceived)}");
            try
            {
                _methodHandler?.Invoke(methodRequestInternal);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodRequestInternal, $"{nameof(OnMethodReceived)}");
            }
        }

        //public async Task DisableMethodsAsync(TimeSpan timeout)
        //{

        //    if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableMethodsAsync)}");

        //    Debug.Assert(_methodSendingLink != null);
        //    Debug.Assert(_methodReceivingLink != null);

        //    try
        //    {
        //        ICollection<Task> tasks = new List<Task>();
        //        if (_methodReceivingLink != null)
        //        {
        //            tasks.Add(_methodReceivingLink.CloseAsync(timeout));
        //        }

        //        if (_methodSendingLink != null)
        //        {
        //            tasks.Add(_methodSendingLink.CloseAsync(timeout));
        //        }

        //        if (tasks.Count > 0)
        //        {
        //            await Task.WhenAll(tasks).ConfigureAwait(false);
        //            _methodReceivingLink = null;
        //            _methodSendingLink = null;
        //        }
        //    }
        //    finally
        //    {
        //        if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(DisableMethodsAsync)}");
        //    }
        //}

        public async Task<AmqpIoTOutcome> SendMethodResponseAsync(MethodResponseInternal methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodResponse, $"{nameof(SendMethodResponseAsync)}");
            await EnableMethodsAsync(timeout).ConfigureAwait(false);
            Debug.Assert(_methodSendingLink != null);

            try
            {
                return await _methodSendingLink.SendMethodResponseAsync(methodResponse, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodResponse, $"{nameof(SendMethodResponseAsync)}");
            }
        }
        #endregion

        #region Twin
        internal async Task EnableTwinLinksAsync(TimeSpan timeout)
        {
            if (_closed)
            {
                throw DEVICE_DISCONNECTED_EXCEPTION;
            }

            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableTwinLinksAsync)}");
            AmqpIoTSession amqpIoTSession = await EnsureSessionAsync(timeout).ConfigureAwait(false);
            bool gain = await _twinLinksLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            try
            {
                string correlationIdSuffix = Guid.NewGuid().ToString();

                await Task.WhenAll(
                   OpenTwinReceiverLinkAsync(amqpIoTSession, correlationIdSuffix, timeout),
                   OpenTwinSenderLinkAsync(amqpIoTSession, correlationIdSuffix, timeout)
               ).ConfigureAwait(false);
            }
            finally
            {
                _twinLinksLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableTwinLinksAsync)}");
            }
        }

        private async Task OpenTwinReceiverLinkAsync(AmqpIoTSession amqpIoTSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_twinReceivingLink == null || _twinReceivingLink.IsClosing())
            {
                _twinReceivingLink = await amqpIoTSession.OpenTwinReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout).ConfigureAwait(false);
                _twinReceivingLink.Closed += (obj, arg) => {
                    amqpIoTSession.SafeClose();
                };
                _twinReceivingLink.RegisterTwinListener(OnDesiredPropertyReceived);
                if (Logging.IsEnabled) Logging.Associate(this, _twinReceivingLink, $"{nameof(_twinReceivingLink)}");
            }
        }
        private async Task OpenTwinSenderLinkAsync(AmqpIoTSession amqpIoTSession, string correlationIdSuffix, TimeSpan timeout)
        {
            if (_twinSendingLink == null || _twinSendingLink.IsClosing())
            {
                _twinSendingLink = await amqpIoTSession.OpenTwinSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout).ConfigureAwait(false);
                _twinSendingLink.Closed += (obj, arg) => {
                    amqpIoTSession.SafeClose();
                };
                if (Logging.IsEnabled) Logging.Associate(this, _twinSendingLink, $"{nameof(_twinSendingLink)}");
            }
        }

        private void OnDesiredPropertyReceived(Twin twin, string correlationId, TwinCollection twinCollection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, twin, $"{nameof(OnDesiredPropertyReceived)}");
            try
            {
                _twinMessageListener?.Invoke(twin, correlationId, twinCollection);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, twin, $"{nameof(OnDesiredPropertyReceived)}");
            }
        }

        public async Task SendTwinMessageAsync(AmqpTwinMessageType amqpTwinMessageType, string correlationId, TwinCollection reportedProperties, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            await EnableTwinLinksAsync(timeout).ConfigureAwait(false);
            Debug.Assert(_twinSendingLink != null);

            try
            {
                AmqpIoTOutcome amqpIoTOutcome;
                switch (amqpTwinMessageType)
                {
                    case AmqpTwinMessageType.Get:
                        amqpIoTOutcome = await _twinSendingLink.SendTwinGetMessageAsync(correlationId, reportedProperties, timeout).ConfigureAwait(false);
                        if (amqpIoTOutcome != null)
                        {
                            amqpIoTOutcome.ThrowIfNotAccepted();
                        }
                        break;
                    case AmqpTwinMessageType.Patch:
                        amqpIoTOutcome = await _twinSendingLink.SendTwinPatchMessageAsync(correlationId, reportedProperties, timeout).ConfigureAwait(false);
                        if (amqpIoTOutcome != null)
                        {
                            amqpIoTOutcome.ThrowIfNotAccepted();
                        }
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            }
        }
        #endregion

        #region Connectivity Event
        public void OnConnectionDisconnected()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnConnectionDisconnected)}");
            _amqpAuthenticationRefresher?.StopLoop();
            _onUnitDisconnected();
            
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnConnectionDisconnected)}");
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnSessionDisconnected)}");
            if (ReferenceEquals(o, _amqpIoTSession))
            {
                _onUnitDisconnected();
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnSessionDisconnected)}");
        }
        #endregion

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
            else
            {
                _disposed = true;
            }

            if (disposing)
            {
                if (Logging.IsEnabled) Logging.Enter(this, disposing, $"{nameof(Dispose)}");
                _amqpIoTSession?.SafeClose();
                _amqpAuthenticationRefresher?.StopLoop();
                if (Logging.IsEnabled) Logging.Exit(this, disposing, $"{nameof(Dispose)}");
            }
        }
        #endregion
    }
}
