// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Common;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    //
    // Note on ConfigureAwait: dotNetty is using a custom TaskScheduler that binds Tasks to the corresponding
    // EventLoop. To limit I/O to the EventLoopGroup and keep Netty semantics, we are going to ensure that the
    // task continuations are executed by this scheduler using ConfigureAwait(true).
    //
    internal sealed class MqttIotHubAdapter : ChannelHandlerAdapter
    {
        [Flags]
        private enum StateFlags
        {
            NotConnected = 0,
            Connecting = 1,
            Connected = 2,
            Closed = 16
        }

        private const string DeviceCommandTopicFilterFormat = "devices/{0}/messages/devicebound/#";
        private const string DeviceTelemetryTopicFormat = "devices/{0}/messages/events/";
        private const string ModuleTelemetryTopicFormat = "devices/{0}/modules/{1}/messages/events/";
        private const string ApiVersionParam = "api-version";
        private const string DeviceClientTypeParam = "DeviceClientType";
        private const string AuthChainParam = "auth-chain";
        private const string ModelIdParam = "model-id";
        private const char SegmentSeparatorChar = '/';
        private const char SingleSegmentWildcardChar = '+';
        private const char MultiSegmentWildcardChar = '#';
        private static readonly char[] s_wildcardChars = { MultiSegmentWildcardChar, SingleSegmentWildcardChar };
        private const string IotHubTrueString = "true";
        private const string SegmentSeparator = "/";
        private const int MaxPayloadSize = 0x3ffff;
        private const int MaxTopicNameLength = 0xffff;

        private static readonly Action<object> s_pingServerCallback = PingServer;
        private static readonly Action<object> s_checkConnAckTimeoutCallback = ShutdownIfNotReady;
        private static readonly Func<IChannelHandlerContext, Exception, bool> s_shutdownOnWriteErrorHandler = (ctx, ex) => { ShutdownOnError(ctx, ex); return false; };

        private readonly IMqttIotHubEventHandler _mqttIotHubEventHandler;

        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly SimpleWorkQueue<PublishPacket> _deviceBoundOneWayProcessor;
        private readonly OrderedTwoPhaseWorkQueue<int, PublishPacket> _deviceBoundTwoWayProcessor;
        private readonly string _iotHubHostName;
        private readonly MqttTransportSettings _mqttTransportSettings;
        private readonly TimeSpan _pingRequestInterval;
        private readonly IAuthorizationProvider _passwordProvider;
        private readonly SimpleWorkQueue<PublishWorkItem> _serviceBoundOneWayProcessor;
        private readonly OrderedTwoPhaseWorkQueue<int, PublishWorkItem> _serviceBoundTwoWayProcessor;
        private readonly IWillMessage _willMessage;

        private DateTime _lastChannelActivityTime;
        private StateFlags _stateFlags;

        private ConcurrentDictionary<int, TaskCompletionSource> _subscribeCompletions = new ConcurrentDictionary<int, TaskCompletionSource>();
        private ConcurrentDictionary<int, TaskCompletionSource> _unsubscribeCompletions = new ConcurrentDictionary<int, TaskCompletionSource>();

        private int InboundBacklogSize => _deviceBoundOneWayProcessor.BacklogSize + _deviceBoundTwoWayProcessor.BacklogSize;

        private readonly ProductInfo _productInfo;
        private readonly ClientOptions _options;

        private ushort _packetId = 0;
        private SpinLock _packetIdLock = new SpinLock();

        public MqttIotHubAdapter(
            string deviceId,
            string moduleId,
            string iotHubHostName,
            IAuthorizationProvider passwordProvider,
            MqttTransportSettings mqttTransportSettings,
            IWillMessage willMessage,
            IMqttIotHubEventHandler mqttIotHubEventHandler,
            ProductInfo productInfo,
            ClientOptions options)
        {
            Contract.Requires(deviceId != null);
            Contract.Requires(iotHubHostName != null);
            Contract.Requires(passwordProvider != null);
            Contract.Requires(mqttTransportSettings != null);
            Contract.Requires(!mqttTransportSettings.HasWill || willMessage != null);
            Contract.Requires(productInfo != null);

            _deviceId = deviceId;
            _moduleId = moduleId;
            _iotHubHostName = iotHubHostName;
            _passwordProvider = passwordProvider;
            _mqttTransportSettings = mqttTransportSettings;
            _willMessage = willMessage;
            _mqttIotHubEventHandler = mqttIotHubEventHandler;
            _pingRequestInterval = TimeSpan.FromSeconds(_mqttTransportSettings.KeepAliveInSeconds / 4d);
            _productInfo = productInfo;
            _options = options;

            _deviceBoundOneWayProcessor = new SimpleWorkQueue<PublishPacket>(AcceptMessageAsync);
            _deviceBoundTwoWayProcessor = new OrderedTwoPhaseWorkQueue<int, PublishPacket>(AcceptMessageAsync, p => p.PacketId, SendAckAsync);

            _serviceBoundOneWayProcessor = new SimpleWorkQueue<PublishWorkItem>(this.SendMessageToServerAsync);
            _serviceBoundTwoWayProcessor = new OrderedTwoPhaseWorkQueue<int, PublishWorkItem>(SendMessageToServerAsync, p => p.Value.PacketId, ProcessAckAsync);
        }

        #region IChannelHandler overrides

        public override async void ChannelActive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelActive));

            _stateFlags = StateFlags.NotConnected;

            await ConnectAsync(context).ConfigureAwait(true);

            base.ChannelActive(context);

            context.Read();

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelActive));
        }

        public override async Task WriteAsync(IChannelHandlerContext context, object data)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, data, nameof(WriteAsync));

            try
            {
                if (IsInState(StateFlags.Closed))
                {
                    throw new IotHubCommunicationException("MQTT is disconnected.");
                }

                if (data is Message message)
                {
                    await SendMessageAsync(context, message).ConfigureAwait(true);
                    return;
                }

                if (data is string packetIdString)
                {
                    await AcknowledgeAsync(context, packetIdString).ConfigureAwait(true);
                    return;
                }

                if (data is DisconnectPacket)
                {
                    await WriteMessageAsync(context, data, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);
                    return;
                }

                if (data is SubscribePacket)
                {
                    await SubscribeAsync(context, data as SubscribePacket).ConfigureAwait(true);
                    return;
                }

                if (data is UnsubscribePacket)
                {
                    await UnSubscribeAsync(context, data as UnsubscribePacket).ConfigureAwait(true);
                    return;
                }

                throw new InvalidOperationException($"Unexpected data type: '{data.GetType().Name}'");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(WriteAsync));
            }
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, message, nameof(ChannelRead));

            if (!(message is Packet packet))
            {
                return;
            }

            _lastChannelActivityTime = DateTime.UtcNow; // notice last client activity - used in handling disconnects on keep-alive timeout

            if (IsInState(StateFlags.Connected) || (IsInState(StateFlags.Connecting) && packet.PacketType == PacketType.CONNACK))
            {
                ProcessMessage(context, packet);
            }
            else
            {
                // we did not start processing CONNACK yet which means we haven't received it yet but the packet of different type has arrived.
                ShutdownOnError(context, new InvalidOperationException($"Invalid state: {_stateFlags}, packet: {packet.PacketType}"));
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelRead));
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelReadComplete));

            base.ChannelReadComplete(context);
            if (InboundBacklogSize < _mqttTransportSettings.MaxPendingInboundMessages)
            {
                context.Read();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelReadComplete));
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelInactive));

            if (_mqttIotHubEventHandler.State == TransportState.Closed)
            {
                Shutdown(context);
                base.ChannelInactive(context);
            }
            else
            {
                ShutdownOnError(context, new SocketException((int)SocketError.ConnectionReset));
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelInactive));
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, exception.ToString(), nameof(ExceptionCaught));

            ShutdownOnError(context, exception);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, exception.ToString(), nameof(ExceptionCaught));
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object @event)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, @event, nameof(UserEventTriggered));

            var handshakeCompletionEvent = @event as TlsHandshakeCompletionEvent;
            if (handshakeCompletionEvent != null && !handshakeCompletionEvent.IsSuccessful)
            {
                ShutdownOnError(context, handshakeCompletionEvent.Exception);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, @event, nameof(UserEventTriggered));
        }

        #endregion IChannelHandler overrides

        #region Connect

        private async Task ConnectAsync(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ConnectAsync));

            try
            {
                string id = string.IsNullOrWhiteSpace(_moduleId) ? _deviceId : $"{_deviceId}/{_moduleId}";
                string password = null;
                if (_passwordProvider != null)
                {
                    password = await _passwordProvider.GetPasswordAsync().ConfigureAwait(true);
                }
                else
                {
                    Debug.Assert(_mqttTransportSettings.ClientCertificate != null);
                }

                // This check is added to enable the device or module client to available plug and play features. For devices or modules that pass in the model Id,
                // the SDK will enable plug and play features by using the PnP-enabled service API version, and appending the model Id to the MQTT CONNECT packet (in the username).
                // For devices or modules that do not have the model Id set, the SDK will use the GA service API version.
                string serviceParams = null;
                if (string.IsNullOrWhiteSpace(_options?.ModelId))
                {
                    serviceParams = ClientApiVersionHelper.ApiVersionQueryStringLatest;
                }
                else
                {
                    serviceParams = $"{ClientApiVersionHelper.ApiVersionQueryStringPreview}&{ModelIdParam}={_options.ModelId}";
                }

                string usernameString = $"{this._iotHubHostName}/{id}/?{serviceParams}&{DeviceClientTypeParam}={Uri.EscapeDataString(this._productInfo.ToString())}";

                if (!_mqttTransportSettings.AuthenticationChain.IsNullOrWhiteSpace())
                {
                    usernameString += $"&{AuthChainParam}={Uri.EscapeDataString(_mqttTransportSettings.AuthenticationChain)}";
                }

                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(usernameString)}={usernameString}", nameof(ConnectAsync));

                var connectPacket = new ConnectPacket
                {
                    ClientId = id,
                    HasUsername = true,
                    Username = usernameString,
                    HasPassword = !string.IsNullOrEmpty(password),
                    Password = password,
                    KeepAliveInSeconds = _mqttTransportSettings.KeepAliveInSeconds,
                    CleanSession = _mqttTransportSettings.CleanSession,
                    HasWill = _mqttTransportSettings.HasWill
                };
                if (connectPacket.HasWill)
                {
                    Message message = _willMessage.Message;
                    QualityOfService publishToServerQoS = _mqttTransportSettings.PublishToServerQoS;
                    string topicName = GetTelemetryTopicName();
                    PublishPacket will = await ComposePublishPacketAsync(context, message, publishToServerQoS, topicName).ConfigureAwait(true);

                    connectPacket.WillMessage = will.Payload;
                    connectPacket.WillQualityOfService = _willMessage.QoS;
                    connectPacket.WillRetain = false;
                    connectPacket.WillTopicName = will.TopicName;
                }
                _stateFlags = StateFlags.Connecting;

                ScheduleCheckConnectTimeout(context);
                await WriteMessageAsync(context, connectPacket, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);
                _lastChannelActivityTime = DateTime.UtcNow;
                ScheduleKeepConnectionAlive(context);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ConnectAsync));
            }
        }

        private async void ScheduleKeepConnectionAlive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ScheduleKeepConnectionAlive));

            try
            {
                await context.Channel.EventLoop.ScheduleAsync(s_pingServerCallback, context, _pingRequestInterval).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ScheduleKeepConnectionAlive));
        }

        private async void ScheduleCheckConnectTimeout(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ScheduleCheckConnectTimeout));

            try
            {
                await context.Channel.EventLoop.ScheduleAsync(s_checkConnAckTimeoutCallback, context, _mqttTransportSettings.ConnectArrivalTimeout).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ScheduleCheckConnectTimeout));
        }

        private static void ShutdownIfNotReady(object state)
        {
            var context = (IChannelHandlerContext)state;
            var handler = (MqttIotHubAdapter)context.Handler;
            if (handler.IsInState(StateFlags.Connecting))
            {
                ShutdownOnError(context, new TimeoutException("Connection hasn't been established in time."));
            }
        }

        private static async void PingServer(object ctx)
        {
            var context = (IChannelHandlerContext)ctx;
            try
            {
                var self = (MqttIotHubAdapter)context.Handler;

                if (!self.IsInState(StateFlags.Connected))
                {
                    return;
                }

                TimeSpan idleTime = DateTime.UtcNow - self._lastChannelActivityTime;

                if (idleTime > self._pingRequestInterval)
                {
                    // We've been idle for too long, send a ping!
                    await WriteMessageAsync(context, PingReqPacket.Instance, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);

                    if (Logging.IsEnabled) Logging.Info(context, $"Idle time was {idleTime}, so ping request was sent.", nameof(PingServer));
                }

                self.ScheduleKeepConnectionAlive(context);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
        }

        private async Task ProcessConnectAckAsync(IChannelHandlerContext context, ConnAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessConnectAckAsync));

            if (packet.ReturnCode != ConnectReturnCode.Accepted)
            {
                string reason = "CONNECT failed: " + packet.ReturnCode;
                var iotHubException = new UnauthorizedException(reason);
                ShutdownOnError(context, iotHubException);
                return;
            }

            if (!IsInState(StateFlags.Connecting))
            {
                string reason = "CONNECT has been received, however a session has already been established. Only one CONNECT/CONNACK pair is expected per session.";
                var iotHubException = new IotHubException(reason);
                ShutdownOnError(context, iotHubException);
                return;
            }

            _stateFlags = StateFlags.Connected;

            _mqttIotHubEventHandler.OnConnected();

            ResumeReadingIfNecessary(context);

            if (packet.SessionPresent)
            {
                await SubscribeAsync(context, null).ConfigureAwait(true);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessConnectAckAsync));
        }

        private void ProcessPingResponse(IChannelHandlerContext context, PingRespPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessPingResponse));

            /*if (packet.ReturnCode != ConnectReturnCode.Accepted)
            {
                string reason = "CONNECT failed: " + packet.ReturnCode;
                var iotHubException = new UnauthorizedException(reason);
                ShutdownOnError(context, iotHubException);
                return;
            }

            if (!this.IsInState(StateFlags.Connecting))
            {
                string reason = "CONNECT has been received, however a session has already been established. Only one CONNECT/CONNACK pair is expected per session.";
                var iotHubException = new IotHubException(reason);
                ShutdownOnError(context, iotHubException);
                return;
            }

            this.stateFlags = StateFlags.Connected;

            this.mqttIotHubEventHandler.OnConnected();

            this.ResumeReadingIfNecessary(context);

            if (packet.SessionPresent)
            {
                await this.SubscribeAsync(context, null).ConfigureAwait(true);
            }*/

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessPingResponse));
        }

        #endregion Connect

        #region Subscribe

        private async Task SubscribeAsync(IChannelHandlerContext context, SubscribePacket packetPassed)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetPassed, nameof(SubscribeAsync));

            string topicFilter;
            QualityOfService qos;

            if (packetPassed == null || packetPassed.Requests == null)
            {
                topicFilter = GetCommandTopicFilter();
                qos = _mqttTransportSettings.ReceivingQoS;
            }
            else if (packetPassed.Requests.Count == 1)
            {
                topicFilter = packetPassed.Requests[0].TopicFilter;
                qos = packetPassed.Requests[0].QualityOfService;
            }
            else
            {
                throw new ArgumentException("unexpected request count.  expected 1, got " + packetPassed.Requests.Count.ToString());
            }

            if (!string.IsNullOrEmpty(topicFilter))
            {
                int packetId = GetNextPacketId();
                var subscribePacket = new SubscribePacket(packetId, new SubscriptionRequest(topicFilter, qos));
                var subscribeCompletion = new TaskCompletionSource();
                _subscribeCompletions[packetId] = subscribeCompletion;

                await WriteMessageAsync(context, subscribePacket, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);

                await subscribeCompletion.Task.ConfigureAwait(true);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetPassed, nameof(SubscribeAsync));
        }

        private void ProcessSubAck(IChannelHandlerContext context, SubAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessSubAck));

            Contract.Assert(packet != null);


            if (_subscribeCompletions.TryRemove(packet.PacketId, out TaskCompletionSource task))
            {
                task.TryComplete();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessSubAck));
        }

        #endregion Subscribe

        #region Unsubscribe

        private async Task UnSubscribeAsync(IChannelHandlerContext context, UnsubscribePacket packetPassed)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetPassed, nameof(UnSubscribeAsync));

            Contract.Assert(packetPassed != null);

            int packetId = GetNextPacketId();
            packetPassed.PacketId = packetId;

            var unsubscribeCompletion = new TaskCompletionSource();
            _unsubscribeCompletions[packetId] = unsubscribeCompletion;

            await WriteMessageAsync(context, packetPassed, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);

            await unsubscribeCompletion.Task.ConfigureAwait(true);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetPassed, nameof(UnSubscribeAsync));
        }

        private void ProcessUnsubAck(IChannelHandlerContext context, UnsubAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessUnsubAck));

            Contract.Assert(packet != null);

            TaskCompletionSource task;
            if (_unsubscribeCompletions.TryRemove(packet.PacketId, out task))
            {
                task.TryComplete();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessUnsubAck));
        }

        #endregion Unsubscribe

        #region Receiving

        private async void ProcessMessage(IChannelHandlerContext context, Packet packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet.PacketType, nameof(ProcessMessage));

            try
            {
                switch (packet.PacketType)
                {
                    case PacketType.CONNACK:
                        await ProcessConnectAckAsync(context, (ConnAckPacket)packet).ConfigureAwait(true);
                        break;

                    case PacketType.SUBACK:
                        ProcessSubAck(context, packet as SubAckPacket);
                        break;

                    case PacketType.PUBLISH:
                        ProcessPublish(context, (PublishPacket)packet);
                        break;

                    case PacketType.PUBACK:
                        await _serviceBoundTwoWayProcessor.CompleteWorkAsync(context, ((PubAckPacket)packet).PacketId).ConfigureAwait(true);
                        break;

                    case PacketType.PINGRESP:
                        ProcessPingResponse(context, packet as PingRespPacket);
                        break;

                    case PacketType.UNSUBACK:
                        ProcessUnsubAck(context, packet as UnsubAckPacket);
                        break;

                    default:
                        ShutdownOnError(context, new InvalidOperationException($"Unexpected packet type {packet.PacketType}"));
                        break;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet.PacketType, nameof(ProcessMessage));
            }
        }

        private Task AcceptMessageAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish, nameof(AcceptMessageAsync));

            Message message;
            try
            {
                var bodyStream = new ReadOnlyByteBufferStream(publish.Payload, true);

                message = new Message(bodyStream, true);

                PopulateMessagePropertiesFromPacket(message, publish);

                message.MqttTopicName = publish.TopicName;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
                return TaskHelpers.CompletedTask;
            }

            _mqttIotHubEventHandler.OnMessageReceived(message);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish, nameof(AcceptMessageAsync));

            return TaskHelpers.CompletedTask;
        }

        private Task ProcessAckAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish?.Value, nameof(ProcessAckAsync));

            publish.Completion.Complete();

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish?.Value, nameof(ProcessAckAsync));

            return TaskHelpers.CompletedTask;
        }

        #endregion Receiving

        #region Sending

        private void ProcessPublish(IChannelHandlerContext context, PublishPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessPublish));

            if (IsInState(StateFlags.Closed))
            {
                throw new IotHubCommunicationException("MQTT is disconnected.");
            }

            switch (packet.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    _deviceBoundOneWayProcessor.Post(context, packet);
                    break;

                case QualityOfService.AtLeastOnce:
                    _deviceBoundTwoWayProcessor.Post(context, packet);
                    break;

                default:
                    throw new NotSupportedException($"Unexpected QoS: '{packet.QualityOfService}'");
            }
            ResumeReadingIfNecessary(context);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessPublish));
        }

        private async Task SendMessageAsync(IChannelHandlerContext context, Message message)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, message, nameof(SendMessageAsync));

            string topicName;
            QualityOfService qos;

            if (string.IsNullOrEmpty(message.MqttTopicName))
            {
                topicName = GetTelemetryTopicName();
                qos = _mqttTransportSettings.PublishToServerQoS;
            }
            else
            {
                topicName = message.MqttTopicName;
                qos = QualityOfService.AtMostOnce;
            }

            PublishPacket packet = await ComposePublishPacketAsync(context, message, qos, topicName).ConfigureAwait(true);
            var publishCompletion = new TaskCompletionSource();
            var workItem = new PublishWorkItem
            {
                Completion = publishCompletion,
                Value = packet
            };

            if (Logging.IsEnabled) Logging.Info(this, $"id={packet.PacketId} qos={packet.QualityOfService} topic={packet.TopicName}", nameof(SendMessageAsync));

            switch (qos)
            {
                case QualityOfService.AtMostOnce:
                    _serviceBoundOneWayProcessor.Post(context, workItem);
                    break;

                case QualityOfService.AtLeastOnce:
                    _serviceBoundTwoWayProcessor.Post(context, workItem);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported telemetry QoS: '{_mqttTransportSettings.PublishToServerQoS}'");
            }

            await publishCompletion.Task.ConfigureAwait(true);
            if (Logging.IsEnabled) Logging.Exit(this, context.Name, message, nameof(SendMessageAsync));
        }

        private Task AcknowledgeAsync(IChannelHandlerContext context, string packetIdString)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetIdString, nameof(AcknowledgeAsync));

            try
            {
                int packetId;
                if (int.TryParse(packetIdString, out packetId))
                {
                    return _deviceBoundTwoWayProcessor.CompleteWorkAsync(context, packetId);
                }

                return TaskHelpers.CompletedTask;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetIdString, nameof(AcknowledgeAsync));
            }
        }

        private Task SendAckAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish, nameof(SendAckAsync));

            try
            {
                ResumeReadingIfNecessary(context);
                return WriteMessageAsync(context, PubAckPacket.InResponseTo(publish), s_shutdownOnWriteErrorHandler);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish, nameof(SendAckAsync));
            }
        }

        private async Task SendMessageToServerAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish?.Value, nameof(SendMessageToServerAsync));

            if (!IsInState(StateFlags.Connected))
            {
                publish.Completion.TrySetCanceled();
            }
            try
            {
                await WriteMessageAsync(context, publish.Value, s_shutdownOnWriteErrorHandler).ConfigureAwait(true);
                if (publish.Value.QualityOfService == QualityOfService.AtMostOnce)
                {
                    publish.Completion.TryComplete();
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Error(context.Handler, $"Context: {context.Name}: {ex}", nameof(SendMessageToServerAsync));

                publish.Completion.TrySetException(ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish?.Value, nameof(SendMessageToServerAsync));
            }
        }

        #endregion Sending

        #region Shutdown

        private static async void ShutdownOnError(IChannelHandlerContext context, Exception exception)
        {
            if (Logging.IsEnabled) Logging.Enter(context.Handler, context.Name, exception.ToString(), nameof(ShutdownOnError));

            var self = (MqttIotHubAdapter)context.Handler;
            if (!self.IsInState(StateFlags.Closed))
            {
                self._stateFlags |= StateFlags.Closed;
                foreach (var task in self._subscribeCompletions.Values)
                {
                    task.TrySetException(exception);
                }
                self._deviceBoundOneWayProcessor.Abort(exception);
                self._deviceBoundTwoWayProcessor.Abort(exception);
                self._serviceBoundOneWayProcessor.Abort(exception);
                self._serviceBoundTwoWayProcessor.Abort(exception);
                self._mqttIotHubEventHandler.OnError(exception);
                try
                {
                    await context.CloseAsync().ConfigureAwait(true);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    //ignored
                    if (Logging.IsEnabled) Logging.Error(context.Handler, $"Context: {context.Name}: {exception.ToString()}", nameof(ShutdownOnError));
                }
            }

            if (Logging.IsEnabled) Logging.Exit(context.Handler, context.Name, nameof(ShutdownOnError));
        }

        private async void Shutdown(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(Shutdown));

            if (this.IsInState(StateFlags.Closed))
            {
                return;
            }

            try
            {
                this._stateFlags |= StateFlags.Closed; // "or" not to interfere with ongoing logic which has to honor Closed state when it's right time to do (case by case)

                this.CloseIotHubConnection();
                await context.CloseAsync().ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                //ignored
                if (Logging.IsEnabled) Logging.Error(context.Handler, $"Context: {context.Name}: {ex.ToString()}", nameof(Shutdown));
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(Shutdown));
            }
        }

        private async void CloseIotHubConnection()
        {
            if (Logging.IsEnabled) Logging.Enter(this, nameof(CloseIotHubConnection));

            try
            {
                if (this.IsInState(StateFlags.NotConnected) || this.IsInState(StateFlags.Connecting))
                {
                    // closure has happened before IoT Hub connection was established or it was initiated due to disconnect
                    return;
                }

                this._serviceBoundOneWayProcessor.Complete();
                this._deviceBoundOneWayProcessor.Complete();
                this._serviceBoundTwoWayProcessor.Complete();
                this._deviceBoundTwoWayProcessor.Complete();
                await Task.WhenAll(
                    this._serviceBoundOneWayProcessor.Completion,
                    this._serviceBoundTwoWayProcessor.Completion,
                    this._deviceBoundOneWayProcessor.Completion,
                    this._deviceBoundTwoWayProcessor.Completion).ConfigureAwait(true);
            }
            catch (Exception completeEx) when (!completeEx.IsFatal())
            {
                if (Logging.IsEnabled) Logging.Error(this, $"Complete exception: {completeEx.ToString()}", nameof(CloseIotHubConnection));
            }
            finally
            {
                // Fix race condition, cleanup processors to make sure no task hanging
                try
                {
                    this._serviceBoundOneWayProcessor.Abort();
                    this._deviceBoundOneWayProcessor.Abort();
                    this._serviceBoundTwoWayProcessor.Abort();
                    this._deviceBoundTwoWayProcessor.Abort();
                }
                catch (Exception abortEx) when (!abortEx.IsFatal())
                {
                    // ignored on closing
                    if (Logging.IsEnabled) Logging.Error(this, $"Abort exception: {abortEx.ToString()}", nameof(CloseIotHubConnection));
                }

                if (Logging.IsEnabled) Logging.Exit(this, nameof(CloseIotHubConnection));
            }
        }

        #endregion Shutdown

        #region helper methods

        private void ResumeReadingIfNecessary(IChannelHandlerContext context)
        {
            if (InboundBacklogSize == _mqttTransportSettings.MaxPendingInboundMessages - 1) // we picked up a packet from full queue - now we have more room so order another read
            {
                context.Read();
            }
        }

        private bool IsInState(StateFlags stateFlagsToCheck)
        {
            return _stateFlags.HasFlag(stateFlagsToCheck);
        }

        private IByteBuffer GetWillMessageBody(Message message)
        {
            Stream bodyStream = message.GetBodyStream();
            byte[] buffer = new byte[bodyStream.Length];
            bodyStream.Read(buffer, 0, buffer.Length);
            IByteBuffer copiedBuffer = Unpooled.CopiedBuffer(buffer);
            return copiedBuffer;
        }

        private string GetTelemetryTopicName()
        {
            string topicName = string.IsNullOrWhiteSpace(_moduleId)
                ? DeviceTelemetryTopicFormat.FormatInvariant(_deviceId)
                : ModuleTelemetryTopicFormat.FormatInvariant(_deviceId, _moduleId);
            return topicName;
        }

        private string GetCommandTopicFilter()
        {
            string topicFilter = string.IsNullOrWhiteSpace(_moduleId)
                ? DeviceCommandTopicFilterFormat.FormatInvariant(_deviceId)
                : string.Empty;
            return topicFilter;
        }

        private ushort GetNextPacketId()
        {
            bool lockTaken = false;
            ushort ret;

            _packetIdLock.Enter(ref lockTaken);

            Debug.Assert(lockTaken);
            unchecked
            {
                _packetId = (ushort)(++_packetId % 0x8000);
                ret = _packetId == 0 ? ++_packetId : _packetId;
            }
            _packetIdLock.Exit();

            return ret;
        }

        public async Task<PublishPacket> ComposePublishPacketAsync(IChannelHandlerContext context, Message message, QualityOfService qos, string topicName)
        {
            var packet = new PublishPacket(qos, false, false)
            {
                TopicName = PopulateMessagePropertiesFromMessage(topicName, message)
            };
            if (qos > QualityOfService.AtMostOnce)
            {
                int packetId = GetNextPacketId();
                switch (qos)
                {
                    case QualityOfService.AtLeastOnce:
                        packetId &= 0x7FFF; // clear 15th bit
                        break;

                    case QualityOfService.ExactlyOnce:
                        packetId |= 0x8000; // set 15th bit
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(qos), qos, null);
                }
                packet.PacketId = packetId;
            }
            Stream payloadStream = message.GetBodyStream();
            long streamLength = payloadStream.Length;
            if (streamLength > MaxPayloadSize)
            {
                throw new InvalidOperationException($"Message size ({streamLength} bytes) is too big to process. Maximum allowed payload size is {MaxPayloadSize}");
            }

            int length = (int)streamLength;
            IByteBuffer buffer = context.Channel.Allocator.Buffer(length, length);
            await buffer.WriteBytesAsync(payloadStream, length).ConfigureAwait(false);
            Contract.Assert(buffer.ReadableBytes == length);

            packet.Payload = buffer;
            return packet;
        }

        public static void PopulateMessagePropertiesFromPacket(Message message, PublishPacket publish)
        {
            message.LockToken = publish.QualityOfService == QualityOfService.AtLeastOnce ? publish.PacketId.ToString(CultureInfo.InvariantCulture) : null;

            // Device bound messages could be in 2 formats, depending on whether it is going to the device, or to a module endpoint
            // Format 1 - going to the device - devices/{deviceId}/messages/devicebound/{properties}/
            // Format 2 - going to module endpoint - devices/{deviceId}/modules/{moduleId/endpoints/{endpointId}/{properties}/
            // So choose the right format to deserialize properties.
            string[] topicSegments = publish.TopicName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string propertiesSegment = topicSegments.Length > 6 ? topicSegments[6] : topicSegments[4];

            Dictionary<string, string> properties = UrlEncodedDictionarySerializer.Deserialize(propertiesSegment, 0);
            foreach (KeyValuePair<string, string> property in properties)
            {
                if (s_toSystemPropertiesMap.TryGetValue(property.Key, out string propertyName))
                {
                    message.SystemProperties[propertyName] = ConvertToSystemProperty(property);
                }
                else
                {
                    message.Properties[property.Key] = property.Value;
                }
            }
        }

        private static class IotHubWirePropertyNames
        {
            public const string AbsoluteExpiryTime = "$.exp";
            public const string CorrelationId = "$.cid";
            public const string MessageId = "$.mid";
            public const string To = "$.to";
            public const string UserId = "$.uid";
            public const string OutputName = "$.on";
            public const string MessageSchema = "$.schema";
            public const string CreationTimeUtc = "$.ctime";
            public const string ContentType = "$.ct";
            public const string ContentEncoding = "$.ce";
            public const string ConnectionDeviceId = "$.cdid";
            public const string ConnectionModuleId = "$.cmid";
            public const string MqttDiagIdKey = "$.diagid";
            public const string MqttDiagCorrelationContextKey = "$.diagctx";
            public const string InterfaceId = "$.ifid";
            public const string ComponentName = "$.sub";
        }

        private static readonly Dictionary<string, string> s_toSystemPropertiesMap = new Dictionary<string, string>
        {
            {IotHubWirePropertyNames.AbsoluteExpiryTime, MessageSystemPropertyNames.ExpiryTimeUtc},
            {IotHubWirePropertyNames.CorrelationId, MessageSystemPropertyNames.CorrelationId},
            {IotHubWirePropertyNames.MessageId, MessageSystemPropertyNames.MessageId},
            {IotHubWirePropertyNames.To, MessageSystemPropertyNames.To},
            {IotHubWirePropertyNames.UserId, MessageSystemPropertyNames.UserId},
            {IotHubWirePropertyNames.MessageSchema, MessageSystemPropertyNames.MessageSchema},
            {IotHubWirePropertyNames.CreationTimeUtc, MessageSystemPropertyNames.CreationTimeUtc},
            {IotHubWirePropertyNames.ContentType, MessageSystemPropertyNames.ContentType},
            {IotHubWirePropertyNames.ContentEncoding, MessageSystemPropertyNames.ContentEncoding},
            {MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation},
            {MessageSystemPropertyNames.Ack, MessageSystemPropertyNames.Ack},
            {IotHubWirePropertyNames.ConnectionDeviceId, MessageSystemPropertyNames.ConnectionDeviceId },
            {IotHubWirePropertyNames.ConnectionModuleId, MessageSystemPropertyNames.ConnectionModuleId },
            {IotHubWirePropertyNames.MqttDiagIdKey, MessageSystemPropertyNames.DiagId},
            {IotHubWirePropertyNames.MqttDiagCorrelationContextKey, MessageSystemPropertyNames.DiagCorrelationContext},
            {IotHubWirePropertyNames.InterfaceId, MessageSystemPropertyNames.InterfaceId}
        };

        private static readonly Dictionary<string, string> s_fromSystemPropertiesMap = new Dictionary<string, string>
        {
            {MessageSystemPropertyNames.ExpiryTimeUtc, IotHubWirePropertyNames.AbsoluteExpiryTime},
            {MessageSystemPropertyNames.CorrelationId, IotHubWirePropertyNames.CorrelationId},
            {MessageSystemPropertyNames.MessageId, IotHubWirePropertyNames.MessageId},
            {MessageSystemPropertyNames.To, IotHubWirePropertyNames.To},
            {MessageSystemPropertyNames.UserId, IotHubWirePropertyNames.UserId},
            {MessageSystemPropertyNames.MessageSchema, IotHubWirePropertyNames.MessageSchema},
            {MessageSystemPropertyNames.CreationTimeUtc, IotHubWirePropertyNames.CreationTimeUtc},
            {MessageSystemPropertyNames.ContentType, IotHubWirePropertyNames.ContentType},
            {MessageSystemPropertyNames.ContentEncoding, IotHubWirePropertyNames.ContentEncoding},
            {MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation},
            {MessageSystemPropertyNames.Ack, MessageSystemPropertyNames.Ack},
            {MessageSystemPropertyNames.OutputName, IotHubWirePropertyNames.OutputName },
            {MessageSystemPropertyNames.DiagId, IotHubWirePropertyNames.MqttDiagIdKey},
            {MessageSystemPropertyNames.DiagCorrelationContext, IotHubWirePropertyNames.MqttDiagCorrelationContextKey},
            {MessageSystemPropertyNames.InterfaceId, IotHubWirePropertyNames.InterfaceId},
            {MessageSystemPropertyNames.ComponentName,IotHubWirePropertyNames.ComponentName }
        };

        private static string ConvertFromSystemProperties(object systemProperty)
        {
            if (systemProperty is string)
            {
                return (string)systemProperty;
            }
            if (systemProperty is DateTime)
            {
                return ((DateTime)systemProperty).ToString("o", CultureInfo.InvariantCulture);
            }
            return systemProperty?.ToString();
        }

        private static object ConvertToSystemProperty(KeyValuePair<string, string> property)
        {
            if (string.IsNullOrEmpty(property.Value))
            {
                return property.Value;
            }
            if (property.Key == IotHubWirePropertyNames.AbsoluteExpiryTime ||
                property.Key == IotHubWirePropertyNames.CreationTimeUtc)
            {
                return DateTime.ParseExact(property.Value, "o", CultureInfo.InvariantCulture);
            }
            if (property.Key == MessageSystemPropertyNames.Ack)
            {
                return Utils.ConvertDeliveryAckTypeFromString(property.Value);
            }
            return property.Value;
        }

        public static bool CheckTopicFilterMatch(string topicName, string topicFilter)
        {
            int topicFilterIndex = 0;
            int topicNameIndex = 0;
            while (topicNameIndex < topicName.Length && topicFilterIndex < topicFilter.Length)
            {
                int wildcardIndex = topicFilter.IndexOfAny(s_wildcardChars, topicFilterIndex);
                if (wildcardIndex == -1)
                {
                    int matchLength = Math.Max(topicFilter.Length - topicFilterIndex, topicName.Length - topicNameIndex);
                    return string.Compare(topicFilter, topicFilterIndex, topicName, topicNameIndex, matchLength, StringComparison.Ordinal) == 0;
                }
                else
                {
                    if (topicFilter[wildcardIndex] == MultiSegmentWildcardChar)
                    {
                        if (wildcardIndex == 0) // special case -- any topic name would match
                        {
                            return true;
                        }
                        else
                        {
                            int matchLength = wildcardIndex - topicFilterIndex - 1;
                            if (string.Compare(topicFilter, topicFilterIndex, topicName, topicNameIndex, matchLength, StringComparison.Ordinal) == 0
                                && (topicName.Length == topicNameIndex + matchLength || (topicName.Length > topicNameIndex + matchLength && topicName[topicNameIndex + matchLength] == SegmentSeparatorChar)))
                            {
                                // paths match up till wildcard and either it is parent topic in hierarchy (one level above # specified) or any child topic under a matching parent topic
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // single segment wildcard
                        int matchLength = wildcardIndex - topicFilterIndex;
                        if (matchLength > 0 && string.Compare(topicFilter, topicFilterIndex, topicName, topicNameIndex, matchLength, StringComparison.Ordinal) != 0)
                        {
                            return false;
                        }
                        topicNameIndex = topicName.IndexOf(SegmentSeparatorChar, topicNameIndex + matchLength);
                        topicFilterIndex = wildcardIndex + 1;
                        if (topicNameIndex == -1)
                        {
                            // there's no more segments following matched one
                            return topicFilterIndex == topicFilter.Length;
                        }
                    }
                }
            }

            return topicFilterIndex == topicFilter.Length && topicNameIndex == topicName.Length;
        }

        public static Message CompleteMessageFromPacket(Message message, PublishPacket packet, MqttTransportSettings mqttTransportSettings)
        {
            message.MessageId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            if (packet.RetainRequested)
            {
                message.Properties[mqttTransportSettings.RetainPropertyName] = IotHubTrueString;
            }
            if (packet.Duplicate)
            {
                message.Properties[mqttTransportSettings.DupPropertyName] = IotHubTrueString;
            }

            return message;
        }

        public static async Task WriteMessageAsync(IChannelHandlerContext context, object message, Func<IChannelHandlerContext, Exception, bool> exceptionHandler)
        {
            try
            {
                await context.WriteAndFlushAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!exceptionHandler(context, ex))
                {
                    throw;
                }
            }
        }

        internal string PopulateMessagePropertiesFromMessage(string topicName, Message message)
        {
            var systemProperties = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> property in message.SystemProperties)
            {
                if (s_fromSystemPropertiesMap.TryGetValue(property.Key, out string propertyName))
                {
                    systemProperties[propertyName] = ConvertFromSystemProperties(property.Value);
                }
            }
            string properties = UrlEncodedDictionarySerializer.Serialize(new ReadOnlyMergeDictionary<string, string>(systemProperties, message.Properties));

            string msg = properties.Length != 0
                ? topicName.EndsWith(SegmentSeparator, StringComparison.Ordinal) ? topicName + properties + SegmentSeparator : topicName + SegmentSeparator + properties
                : topicName;
            if (Encoding.UTF8.GetByteCount(msg) > MaxTopicNameLength)
            {
                throw new MessageTooLargeException($"TopicName for MQTT packet cannot be larger than {MaxTopicNameLength} bytes, current length is {Encoding.UTF8.GetByteCount(msg)}. The probable cause is the list of message.Properties and/or message.systemProperties is too long. Please use AMQP or HTTP.");
            }

            return msg;
        }

        #endregion helper methods
    }
}
