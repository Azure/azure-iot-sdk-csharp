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

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpUnit : IDisposable
    {
        // If the first argument is set to true, we are disconnecting gracefully via CloseAsync.
        public event EventHandler OnUnitDisconnected;
        private readonly DeviceIdentity _deviceIdentity;
        private readonly Func<MethodRequestInternal, Task> _methodHandler;
        private readonly Action<Twin, string, TwinCollection> _twinMessageListener;
        private readonly Func<string, Message, Task> _eventListener;
        private readonly IAmqpSessionCreator _amqpSessionCreator;
        private readonly IAmqpTokenRefresherCreator _amqpAuthenticationRefresherCreator;
        private int _isUsable;
        private bool _disposed;

        private AmqpIoTSendingLink _messageSendingLink;
        private AmqpIoTReceivingLink _messageReceivingLink;
        private readonly SemaphoreSlim _messageReceivingLinkLock = new SemaphoreSlim(1, 1);

        private AmqpIoTSendingLink _methodSendingLink;
        private AmqpIoTReceivingLink _methodReceivingLink;

        private AmqpIoTSendingLink _twinSendingLink;
        private AmqpIoTReceivingLink _twinReceivingLink;
        private bool _twinLinksOpened;
        private readonly SemaphoreSlim _twinLinksLock = new SemaphoreSlim(1, 1);

        // Note: By design, there is no equivalent Module eventSendingLink.
        private AmqpIoTReceivingLink _eventReceivingLink;
        
        private AmqpIoTSession _amqpIoTSession;
        private IAmqpIoTAuthenticationRefresher _amqpAuthenticationRefresher;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public AmqpUnit(
            DeviceIdentity deviceIdentity,
            IAmqpSessionCreator amqpIoTSessionCreator,
            IAmqpTokenRefresherCreator amqpTokenRefresherCreator,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<Twin, string, TwinCollection> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            _deviceIdentity = deviceIdentity;
            _methodHandler = methodHandler;
            _twinMessageListener = twinMessageListener;
            _eventListener = eventListener;
            _amqpSessionCreator = amqpIoTSessionCreator;
            _amqpAuthenticationRefresherCreator = amqpTokenRefresherCreator;

            if (Logging.IsEnabled) Logging.Associate(this, _deviceIdentity, $"{nameof(_deviceIdentity)}");
        }

        #region Usability
        public bool IsUsable()
        {
            return Volatile.Read(ref _isUsable) == 0;
        }

        public int SetNotUsable()
        {
            return Interlocked.Exchange(ref _isUsable, 1);
        }
        #endregion

        #region Open-Close
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenAsync)}");

            try
            {
                Debug.Assert(_amqpIoTSession == null);
                Debug.Assert(IsUsable());

                _amqpIoTSession = await _amqpSessionCreator.CreateSession(
                    _deviceIdentity, 
                    timeout).ConfigureAwait(false);

                if (Logging.IsEnabled) Logging.Associate(this, _amqpIoTSession, $"{nameof(_amqpIoTSession)}");
                await _amqpIoTSession.OpenAsync(timeout).ConfigureAwait(false);
                if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                {
                    _amqpAuthenticationRefresher = await _amqpAuthenticationRefresherCreator.CreateRefresher(_deviceIdentity, timeout).ConfigureAwait(false);
                    if (Logging.IsEnabled) Logging.Associate(this, _amqpAuthenticationRefresher, $"{nameof(_amqpAuthenticationRefresher)}");
                }

                _amqpIoTSession.Closed += OnSessionDisconnected;

                _messageSendingLink = await _amqpIoTSession.OpenTelemetrySenderLinkAsync(
                    _deviceIdentity,
                    timeout).ConfigureAwait(false);
                _messageSendingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, _messageSendingLink, $"{nameof(_messageSendingLink)}");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (SetNotUsable() == 0)
                {
                    OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
                }

                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenAsync)}");
            }
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");

            if (SetNotUsable() == 0 && _amqpIoTSession != null)
            {
                await _amqpIoTSession.CloseAsync(timeout).ConfigureAwait(false);
                OnUnitDisconnected?.Invoke(true, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CloseAsync)}");
        }
        #endregion

        #region Message

        private async Task EnsureReceivingLinkIsOpenedAsync(TimeSpan timeout)
        {
            if (Volatile.Read(ref _messageReceivingLink) != null) return;

            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureReceivingLinkIsOpenedAsync)}");

            try
            {
                await _messageReceivingLinkLock.WaitAsync().ConfigureAwait(false);
                if (_messageReceivingLink != null) return;

                _messageReceivingLink = await _amqpIoTSession.OpenTelemetryReceiverLinkAsync(
                    _deviceIdentity,
                    timeout
                ).ConfigureAwait(false);

                _messageReceivingLink.Closed += OnLinkDisconnected;
                if (Logging.IsEnabled) Logging.Associate(this, this, _messageReceivingLink, $"{nameof(EnsureReceivingLinkIsOpenedAsync)}");
            }
            finally
            {
                _messageReceivingLinkLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureReceivingLinkIsOpenedAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendMessagesAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, messages, timeout, $"{nameof(SendMessagesAsync)}");

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

            try
            {
                await EnsureReceivingLinkIsOpenedAsync(timeout).ConfigureAwait(false);
                Debug.Assert(_messageSendingLink != null);

                return await _messageReceivingLink.ReceiveAmqpMessageAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpIoTExceptionAdapter.ConvertToIoTHubException(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> DisposeMessageAsync(string lockToken, AmqpIoTDisposeActions disposeAction, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, $"{nameof(DisposeMessageAsync)}");

            AmqpIoTOutcome disposeOutcome;
            if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
            {
                disposeOutcome = await _messageReceivingLink.DisposeMessageAsync(lockToken, AmqpIoTResultAdapter.GetResult(disposeAction), timeout).ConfigureAwait(false);
            }
            else
            {
                disposeOutcome = await _messageReceivingLink.DisposeMessageAsync(lockToken, AmqpIoTResultAdapter.GetResult(disposeAction), timeout).ConfigureAwait(false);
            }

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
                Debug.Assert(_eventReceivingLink == null);
                _eventReceivingLink = await _amqpIoTSession.OpenEventsReceiverLinkAsync(
                    _deviceIdentity,
                    timeout
                ).ConfigureAwait(false);

                _eventReceivingLink.RegisterEventListener(OnEventsReceived);
                _eventReceivingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, this, _eventReceivingLink, $"{nameof(EnableEventReceiveAsync)}");
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
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableMethodsAsync)}");

            try
            {
                Debug.Assert(_methodSendingLink == null);
                Debug.Assert(_methodReceivingLink == null);

                string correlationIdSuffix = Guid.NewGuid().ToString();

                Task<AmqpIoTReceivingLink> receiveLinkCreator = _amqpIoTSession.OpenMethodsReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout);
                Task<AmqpIoTSendingLink> sendingLinkCreator = _amqpIoTSession.OpenMethodsSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout);
                await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                _methodReceivingLink = receiveLinkCreator.Result;
                _methodSendingLink = sendingLinkCreator.Result;

                _methodReceivingLink.RegisterMethodListener(OnMethodReceived);
                _methodSendingLink.Closed += OnLinkDisconnected;
                _methodReceivingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, _methodReceivingLink, $"{nameof(_methodReceivingLink)}");
                if (Logging.IsEnabled) Logging.Associate(this, _methodSendingLink, $"{nameof(_methodSendingLink)}");
            }
            catch (Exception)
            {
                _methodReceivingLink?.Abort();
                _methodReceivingLink = null;

                _methodSendingLink?.Abort();
                _methodSendingLink = null;

                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableMethodsAsync)}");
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

        public async Task DisableMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableMethodsAsync)}");

            Debug.Assert(_methodSendingLink != null);
            Debug.Assert(_methodReceivingLink != null);

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
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(DisableMethodsAsync)}");
            }
        }

        public async Task<AmqpIoTOutcome> SendMethodResponseAsync(MethodResponseInternal methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodResponse, $"{nameof(SendMethodResponseAsync)}");

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
        public async Task EnsureTwinLinksAreOpenedAsync(TimeSpan timeout)
        {
            if (Volatile.Read(ref _twinLinksOpened) == true) return;
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureTwinLinksAreOpenedAsync)}");

            try
            {
                await _twinLinksLock.WaitAsync().ConfigureAwait(false);
                if (_twinLinksOpened) return;

                Debug.Assert(_twinSendingLink == null);
                Debug.Assert(_twinReceivingLink == null);

                string correlationIdSuffix = Guid.NewGuid().ToString();

                Task<AmqpIoTReceivingLink> receiveLinkCreator = _amqpIoTSession.OpenTwinReceiverLinkAsync(_deviceIdentity, correlationIdSuffix, timeout);
                Task<AmqpIoTSendingLink> sendingLinkCreator = _amqpIoTSession.OpenTwinSenderLinkAsync(_deviceIdentity, correlationIdSuffix, timeout);
                await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                _twinSendingLink = sendingLinkCreator.Result;
                _twinSendingLink.Closed += OnLinkDisconnected;

                _twinReceivingLink = receiveLinkCreator.Result;
                _twinReceivingLink.RegisterTwinListener(OnDesiredPropertyReceived);
                _twinReceivingLink.Closed += OnLinkDisconnected;

                _twinLinksOpened = true;

                if (Logging.IsEnabled) Logging.Associate(this, this, _twinReceivingLink, $"{nameof(EnsureTwinLinksAreOpenedAsync)}");
                if (Logging.IsEnabled) Logging.Associate(this, this, _twinSendingLink, $"{nameof(EnsureTwinLinksAreOpenedAsync)}");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _twinReceivingLink?.Abort();
                _twinSendingLink?.Abort();
                _twinReceivingLink = null;
                _twinSendingLink = null;

                throw;
            }
            finally
            {
                _twinLinksLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureTwinLinksAreOpenedAsync)}");
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
            if (SetNotUsable() == 0)
            {
                _amqpAuthenticationRefresher?.StopLoop();
                OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnConnectionDisconnected)}");
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnSessionDisconnected)}");

            if (SetNotUsable() == 0)
            {
                _amqpAuthenticationRefresher?.StopLoop();
                OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnSessionDisconnected)}");
        }

        private void OnLinkDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnLinkDisconnected)}");

            if (SetNotUsable() == 0)
            {
                _amqpAuthenticationRefresher?.StopLoop();
                OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnLinkDisconnected)}");
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
            if (_disposed) return;

            if (disposing)
            {
                if (Logging.IsEnabled) Logging.Enter(this, disposing, $"{nameof(Dispose)}");
                if (SetNotUsable() == 0)
                {
                    OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
                }

                _amqpIoTSession?.Abort();
                if (Logging.IsEnabled) Logging.Exit(this, disposing, $"{nameof(Dispose)}");
            }

            _disposed = true;
        }
        #endregion
    }
}
