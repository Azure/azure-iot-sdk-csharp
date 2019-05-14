// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnit : IDisposable
    {
        private const string DeviceStreamingFieldStreamName = "IoThub-streaming-name";
        private const string DeviceStreamingFieldProxyUri = "IoThub-streaming-url";
        private const string DeviceStreamingFieldAuthorizationToken = "IoThub-streaming-auth-token";
        private const string DeviceStreamingFieldIsAccepted = "IoThub-streaming-is-accepted";

        // If the first argument is set to true, we are disconnecting gracefully via CloseAsync.
        public event EventHandler OnUnitDisconnected;
        private readonly DeviceIdentity _deviceIdentity;
        private readonly Func<MethodRequestInternal, Task> _methodHandler;
        private readonly Action<AmqpMessage> _twinMessageListener;
        private readonly Func<string, Message, Task> _eventListener;
        private readonly Func<DeviceIdentity, ILinkFactory, AmqpSessionSettings, TimeSpan, Task<AmqpSession>> _amqpSessionCreator;
        private readonly Func<DeviceIdentity, TimeSpan, Task<IAmqpAuthenticationRefresher>> _amqpAuthenticationRefresherCreator;
        private int _isUsable;
        private bool _disposed;

        private SendingAmqpLink _messageSendingLink;
        private ReceivingAmqpLink _messageReceivingLink;
        private readonly SemaphoreSlim _messageReceivingLinkLock = new SemaphoreSlim(1, 1);

        private SendingAmqpLink _methodSendingLink;
        private ReceivingAmqpLink _methodReceivingLink;

        private SendingAmqpLink _twinSendingLink;
        private ReceivingAmqpLink _twinReceivingLink;
        private bool _twinLinksOpened;
        private readonly SemaphoreSlim _twinLinksLock = new SemaphoreSlim(1, 1);

        private SendingAmqpLink _streamSendingLink;
        private ReceivingAmqpLink _streamReceivingLink;
        private bool _streamLinksOpened;
        private readonly SemaphoreSlim _streamLinksLock = new SemaphoreSlim(1, 1);

        // Note: By design, there is no equivalent Module eventSendingLink.
        private ReceivingAmqpLink _eventReceivingLink;
        
        private AmqpSession _amqpSession;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private AmqpSessionSettings _amqpSessionSettings;

        public AmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<DeviceIdentity, ILinkFactory, AmqpSessionSettings, TimeSpan, Task<AmqpSession>> amqpSessionCreator,
            Func<DeviceIdentity, TimeSpan, Task<IAmqpAuthenticationRefresher>> amqpAuthenticationRefresherCreator,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            _deviceIdentity = deviceIdentity;
            _methodHandler = methodHandler;
            _twinMessageListener = twinMessageListener;
            _eventListener = eventListener;
            _amqpSessionCreator = amqpSessionCreator;
            _amqpAuthenticationRefresherCreator = amqpAuthenticationRefresherCreator;
            _amqpSessionSettings = new AmqpSessionSettings()
             {
                 Properties = new Fields()
             };

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
                Debug.Assert(_amqpSession == null);
                Debug.Assert(IsUsable());

                _amqpSession = await _amqpSessionCreator.Invoke(
                    _deviceIdentity, 
                    AmqpLinkFactory.GetInstance(), 
                    _amqpSessionSettings, 
                    timeout).ConfigureAwait(false);

                if (Logging.IsEnabled) Logging.Associate(this, _amqpSession, $"{nameof(_amqpSession)}");
                await _amqpSession.OpenAsync(timeout).ConfigureAwait(false);
                if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                {
                    _amqpAuthenticationRefresher = await _amqpAuthenticationRefresherCreator.Invoke(_deviceIdentity, timeout).ConfigureAwait(false);
                    if (Logging.IsEnabled) Logging.Associate(this, _amqpAuthenticationRefresher, $"{nameof(_amqpAuthenticationRefresher)}");
                }

                _amqpSession.Closed += OnSessionDisconnected;

                _messageSendingLink = await AmqpLinkHelper.OpenTelemetrySenderLinkAsync(
                    _deviceIdentity,
                    _amqpSession,
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

            if (SetNotUsable() == 0 && _amqpSession != null)
            {
                await _amqpSession.CloseAsync(timeout).ConfigureAwait(false);
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

                _messageReceivingLink = await AmqpLinkHelper.OpenTelemetryReceiverLinkAsync(
                    _deviceIdentity,
                    _amqpSession,
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

        public async Task<Outcome> SendMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendMessageAsync)}");

            try
            {
                Debug.Assert(_messageSendingLink != null);
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(_messageSendingLink, message, timeout).ConfigureAwait(false);
                return outcome;
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

                AmqpMessage amqpMessage = await AmqpLinkHelper.ReceiveAmqpMessageAsync(_messageReceivingLink, timeout).ConfigureAwait(false);
                Message message = null;
                if (amqpMessage != null)
                {
                    message = new Message(amqpMessage)
                    {
                        LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                    };
                }
                return message;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            }
        }

        public async Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, $"{nameof(DisposeMessageAsync)}");

            Outcome disposeOutcome;
            if (_deviceIdentity.IotHubConnectionString.ModuleId.IsNullOrWhiteSpace())
            {
                disposeOutcome = await AmqpLinkHelper.DisposeMessageAsync(_messageReceivingLink, lockToken, outcome, timeout).ConfigureAwait(false);
            }
            else
            {
                disposeOutcome = await AmqpLinkHelper.DisposeMessageAsync(_eventReceivingLink, lockToken, outcome, timeout).ConfigureAwait(false);
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
                _eventReceivingLink = await AmqpLinkHelper.OpenEventsReceiverLinkAsync(
                    _deviceIdentity,
                    _amqpSession,
                    timeout
                ).ConfigureAwait(false);

                _eventReceivingLink.RegisterMessageListener(OnEventsReceived);
                _eventReceivingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, this, _eventReceivingLink, $"{nameof(EnableEventReceiveAsync)}");
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            }
        }

        public async Task<Outcome> SendEventAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendEventAsync)}");
            try
            {
                Outcome outcome = await SendMessageAsync(message, timeout).ConfigureAwait(false);
                return outcome;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendEventAsync)}");
            }
        }

        internal void OnEventsReceived(AmqpMessage amqpMessage)
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

            try
            {
                Debug.Assert(_methodSendingLink == null);
                Debug.Assert(_methodReceivingLink == null);

                string correlationIdSuffix = Guid.NewGuid().ToString();
                Task<ReceivingAmqpLink> receiveLinkCreator = 
                    AmqpLinkHelper.OpenMethodsReceiverLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                Task<SendingAmqpLink> sendingLinkCreator = 
                    AmqpLinkHelper.OpenMethodsSenderLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                _methodReceivingLink = receiveLinkCreator.Result;
                _methodSendingLink = sendingLinkCreator.Result;

                _methodReceivingLink.RegisterMessageListener(OnMethodReceived);
                _methodSendingLink.Closed += OnLinkDisconnected;
                _methodReceivingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, _methodReceivingLink, $"{nameof(_methodReceivingLink)}");
                if (Logging.IsEnabled) Logging.Associate(this, _methodSendingLink, $"{nameof(_methodSendingLink)}");
            }
            catch(Exception)
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
        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            try
            {
                MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
                _methodReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _methodHandler?.Invoke(methodRequestInternal);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
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

        public async Task<Outcome> SendMethodResponseAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, $"{nameof(SendMethodResponseAsync)}");

            Debug.Assert(_methodSendingLink != null);

            try
            {
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(_methodSendingLink, message, timeout).ConfigureAwait(false);
                return outcome;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, $"{nameof(SendMethodResponseAsync)}");
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

                Task<ReceivingAmqpLink> receiveLinkCreator = 
                    AmqpLinkHelper.OpenTwinReceiverLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                Task<SendingAmqpLink> sendingLinkCreator = 
                    AmqpLinkHelper.OpenTwinSenderLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                _twinSendingLink = sendingLinkCreator.Result;
                _twinSendingLink.Closed += OnLinkDisconnected;

                _twinReceivingLink = receiveLinkCreator.Result;
                _twinReceivingLink.RegisterMessageListener(OnDesiredPropertyReceived);
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

        private void OnDesiredPropertyReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            try
            {
                _twinReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _twinMessageListener?.Invoke(amqpMessage);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            }
        }

        public async Task<Outcome> SendTwinMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");

            Debug.Assert(_twinSendingLink != null);

            try
            {
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(_twinSendingLink, message, timeout).ConfigureAwait(false);
                return outcome;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            }
        }
        #endregion

        #region DEVICE STREAMING
        public async Task EnableStreamsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableStreamsAsync)}");

            try
            {
                Debug.Assert(_streamReceivingLink == null);
                Debug.Assert(_streamSendingLink == null);

                string correlationIdSuffix = Guid.NewGuid().ToString();
                Task<ReceivingAmqpLink> receiveLinkCreator =
                    AmqpLinkHelper.OpenStreamsReceiverLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                Task<SendingAmqpLink> sendingLinkCreator =
                    AmqpLinkHelper.OpenStreamsSenderLinkAsync(
                        _deviceIdentity,
                        _amqpSession,
                        correlationIdSuffix,
                        timeout);

                await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                _streamReceivingLink = receiveLinkCreator.Result;
                _streamSendingLink = sendingLinkCreator.Result;

                _streamReceivingLink.Closed += OnLinkDisconnected;
                _streamSendingLink.Closed += OnLinkDisconnected;

                if (Logging.IsEnabled) Logging.Associate(this, _streamReceivingLink, $"{nameof(_streamReceivingLink)}");
                if (Logging.IsEnabled) Logging.Associate(this, _streamSendingLink, $"{nameof(_streamSendingLink)}");
            }
            catch (Exception)
            {
                _streamReceivingLink?.Abort();
                _streamReceivingLink = null;

                _streamReceivingLink?.Abort();
                _streamReceivingLink = null;

                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableStreamsAsync)}");
            }
        }

        public async Task DisableStreamsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableStreamsAsync)}");

            Debug.Assert(_streamSendingLink != null);
            Debug.Assert(_streamReceivingLink != null);

            try
            {
                ICollection<Task> tasks = new List<Task>();
                if (_streamSendingLink != null)
                {
                    tasks.Add(_streamSendingLink.CloseAsync(timeout));
                }

                if (_streamReceivingLink != null)
                {
                    tasks.Add(_streamReceivingLink.CloseAsync(timeout));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    _streamSendingLink = null;
                    _streamReceivingLink = null;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(DisableStreamsAsync)}");
            }
        }

        public async Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(WaitForDeviceStreamRequestAsync)}");

            try
            {
                DeviceStreamRequest deviceStreamRequest = null;
                using (AmqpMessage amqpMessage = await AmqpLinkHelper.ReceiveAmqpMessageAsync(_streamReceivingLink, timeout).ConfigureAwait(false))
                {
                    if (amqpMessage != null)
                    {
                        deviceStreamRequest = ConstructStreamRequestFromAmqpMessage(amqpMessage);
                    }
                    return deviceStreamRequest;
                }
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(WaitForDeviceStreamRequestAsync)}");
            }
        }

        public async Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, TimeSpan timeout)
        {
            if (request == null || request.RequestId == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, request, timeout, $"{nameof(AcceptDeviceStreamRequestAsync)}");

                DeviceStreamResponse response = new DeviceStreamResponse(request.RequestId, true);

                await SendDeviceStreamResponseAsync(response, timeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, timeout, $"{nameof(AcceptDeviceStreamRequestAsync)}");
            }
        }

        public async Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, TimeSpan timeout)
        {
            if (request == null || request.RequestId == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, request, timeout, $"{nameof(RejectDeviceStreamRequestAsync)}");

                DeviceStreamResponse response = new DeviceStreamResponse(request.RequestId, false);

                await SendDeviceStreamResponseAsync(response, timeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, timeout, $"{nameof(RejectDeviceStreamRequestAsync)}");
            }
        }

        public async Task SendDeviceStreamResponseAsync(DeviceStreamResponse streamResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, streamResponse, timeout, $"{nameof(SendDeviceStreamResponseAsync)}");

            try
            {
                Outcome outcome;
                using (AmqpMessage amqpMessage = CreateAmqpMessageFromStreamResponse(streamResponse))
                {
                    outcome = await _streamSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, timeout).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, streamResponse, timeout, $"{nameof(SendDeviceStreamResponseAsync)}");
            }
        }

        private DeviceStreamRequest ConstructStreamRequestFromAmqpMessage(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(ConstructStreamRequestFromAmqpMessage)}");

            if (amqpMessage == null)
            {
                throw new ArgumentNullException(nameof(amqpMessage));
            }

            string streamRequestId = string.Empty;
            string streamName = string.Empty;
            string proxyUri = string.Empty;
            string authorizationToken = string.Empty;

            SectionFlag sections = amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                streamRequestId = amqpMessage.Properties.CorrelationId != null ? amqpMessage.Properties.CorrelationId.ToString() : null;
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                if (!(amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(DeviceStreamingFieldStreamName), out streamName) ?? false))
                {
                    throw new InvalidDataException("Stream name is missing");
                }

                if (!(amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(DeviceStreamingFieldProxyUri), out proxyUri) ?? false))
                {
                    throw new InvalidDataException("Proxy URI is missing");
                }

                if (!(amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(DeviceStreamingFieldAuthorizationToken), out authorizationToken) ?? false))
                {
                    throw new InvalidDataException("Authorization Token is missing");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(ConstructStreamRequestFromAmqpMessage)}");

            return new DeviceStreamRequest(streamRequestId, streamName, new Uri(proxyUri), authorizationToken);
        }

        private AmqpMessage CreateAmqpMessageFromStreamResponse(DeviceStreamResponse streamResponseInternal)
        {
            if (Logging.IsEnabled) Logging.Enter(this, streamResponseInternal, $"{nameof(CreateAmqpMessageFromStreamResponse)}");

            AmqpMessage amqpMessage = AmqpMessage.Create();

            amqpMessage.Properties.CorrelationId = new Guid(streamResponseInternal.RequestId);

            if (amqpMessage.ApplicationProperties == null)
            {
                amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            amqpMessage.ApplicationProperties.Map[DeviceStreamingFieldIsAccepted] = streamResponseInternal.IsAccepted;

            if (Logging.IsEnabled) Logging.Exit(this, streamResponseInternal, $"{nameof(CreateAmqpMessageFromStreamResponse)}");

            return amqpMessage;
        }
        #endregion DEVICE STREAMING

        #region Connectivity Event
        public void OnConnectionDisconnected()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnConnectionDisconnected)}");
            if (SetNotUsable() == 0)
            {
                OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnConnectionDisconnected)}");
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnSessionDisconnected)}");

            if (SetNotUsable() == 0)
            {
                OnUnitDisconnected?.Invoke(false, EventArgs.Empty);
            }

            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnSessionDisconnected)}");
        }

        private void OnLinkDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnLinkDisconnected)}");

            if (SetNotUsable() == 0)
            {
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

                _amqpSession?.Abort();
                if (Logging.IsEnabled) Logging.Exit(this, disposing, $"{nameof(Dispose)}");
            }

            _disposed = true;
        }
        #endregion
    }
}
