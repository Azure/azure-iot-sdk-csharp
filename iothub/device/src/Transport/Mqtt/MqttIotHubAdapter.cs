// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Mqtt.Packets;
    using DotNetty.Common.Concurrency;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Channels;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using System.Diagnostics;
    using Microsoft.Azure.Devices.Shared;

    sealed class MqttIotHubAdapter : ChannelHandlerAdapter
    {
        [Flags]
        enum StateFlags
        {
            NotConnected = 0,
            Connecting = 1,
            Connected = 2,
            Closed = 16
        }
        const string DeviceCommandTopicFilterFormat = "devices/{0}/messages/devicebound/#";
        const string DeviceTelemetryTopicFormat = "devices/{0}/messages/events/";
        const string ModuleTelemetryTopicFormat = "devices/{0}/modules/{1}/messages/events/";


        static readonly Action<object> PingServerCallback = PingServer;
        static readonly Action<object> CheckConnAckTimeoutCallback = ShutdownIfNotReady;
        static readonly Func<IChannelHandlerContext, Exception, bool> ShutdownOnWriteErrorHandler = (ctx, ex) => { ShutdownOnError(ctx, ex); return false; };

        readonly IMqttIotHubEventHandler mqttIotHubEventHandler;

        readonly string deviceId;
        readonly string moduleId;
        readonly SimpleWorkQueue<PublishPacket> deviceBoundOneWayProcessor;
        readonly OrderedTwoPhaseWorkQueue<int, PublishPacket> deviceBoundTwoWayProcessor;
        readonly string iotHubHostName;
        readonly MqttTransportSettings mqttTransportSettings;
        readonly TimeSpan pingRequestInterval;
        readonly IAuthorizationProvider passwordProvider;
        readonly SimpleWorkQueue<PublishWorkItem> serviceBoundOneWayProcessor;
        readonly OrderedTwoPhaseWorkQueue<int, PublishWorkItem> serviceBoundTwoWayProcessor;
        readonly IWillMessage willMessage;

        DateTime lastChannelActivityTime;
        StateFlags stateFlags;

        ConcurrentDictionary<int, TaskCompletionSource> subscribeCompletions = new ConcurrentDictionary<int, TaskCompletionSource>();
        ConcurrentDictionary<int, TaskCompletionSource> unsubscribeCompletions = new ConcurrentDictionary<int, TaskCompletionSource>();

        int InboundBacklogSize => this.deviceBoundOneWayProcessor.BacklogSize + this.deviceBoundTwoWayProcessor.BacklogSize;

        ProductInfo productInfo;

        public MqttIotHubAdapter(
            string deviceId,
            string moduleId,
            string iotHubHostName,
            IAuthorizationProvider passwordProvider,
            MqttTransportSettings mqttTransportSettings,
            IWillMessage willMessage,
            IMqttIotHubEventHandler mqttIotHubEventHandler,
            ProductInfo productInfo)
        {
            Contract.Requires(deviceId != null);
            Contract.Requires(iotHubHostName != null);
            Contract.Requires(passwordProvider != null);
            Contract.Requires(mqttTransportSettings != null);
            Contract.Requires(!mqttTransportSettings.HasWill || willMessage != null);
            Contract.Requires(productInfo != null);

            this.deviceId = deviceId;
            this.moduleId = moduleId;
            this.iotHubHostName = iotHubHostName;
            this.passwordProvider = passwordProvider;
            this.mqttTransportSettings = mqttTransportSettings;
            this.willMessage = willMessage;
            this.mqttIotHubEventHandler = mqttIotHubEventHandler;
            this.pingRequestInterval = this.mqttTransportSettings.KeepAliveInSeconds > 0 ? TimeSpan.FromSeconds(this.mqttTransportSettings.KeepAliveInSeconds / 4d) : TimeSpan.MaxValue;
            this.productInfo = productInfo;

            this.deviceBoundOneWayProcessor = new SimpleWorkQueue<PublishPacket>(this.AcceptMessageAsync);
            this.deviceBoundTwoWayProcessor = new OrderedTwoPhaseWorkQueue<int, PublishPacket>(this.AcceptMessageAsync, p => p.PacketId, this.SendAckAsync);

            this.serviceBoundOneWayProcessor = new SimpleWorkQueue<PublishWorkItem>(this.SendMessageToServerAsync);
            this.serviceBoundTwoWayProcessor = new OrderedTwoPhaseWorkQueue<int, PublishWorkItem>(this.SendMessageToServerAsync, p => p.Value.PacketId, this.ProcessAckAsync);
        }

#region IChannelHandler overrides

        public override void ChannelActive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelActive));

            this.stateFlags = StateFlags.NotConnected;

            this.Connect(context);

            // TODO #223: this executes in parallel with the Connect(context).

            base.ChannelActive(context);

            context.Read();

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelActive));
        }

        public override async Task WriteAsync(IChannelHandlerContext context, object data)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, data, nameof(WriteAsync));

            try
            {
                if (this.IsInState(StateFlags.Closed))
                {
                    return;
                }

                var message = data as Message;
                if (message != null)
                {
                    await this.SendMessageAsync(context, message).ConfigureAwait(false);
                    return;
                }

                string packetIdString = data as string;
                if (packetIdString != null)
                {
                    await this.AcknowledgeAsync(context, packetIdString).ConfigureAwait(false);
                    return;
                }

                if (data is DisconnectPacket)
                {
                    await Util.WriteMessageAsync(context, data, ShutdownOnWriteErrorHandler).ConfigureAwait(false);
                    return;
                }

                if (data is SubscribePacket)
                {
                    await this.SubscribeAsync(context, data as SubscribePacket).ConfigureAwait(false);
                    return;
                }

                if (data is UnsubscribePacket)
                {
                    await this.UnSubscribeAsync(context, data as UnsubscribePacket).ConfigureAwait(false);
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

            var packet = message as Packet;
            if (packet == null)
            {
                return;
            }

            this.lastChannelActivityTime = DateTime.UtcNow; // notice last client activity - used in handling disconnects on keep-alive timeout

            if (this.IsInState(StateFlags.Connected) || (this.IsInState(StateFlags.Connecting) && packet.PacketType == PacketType.CONNACK))
            {
                this.ProcessMessage(context, packet);
            }
            else
            {
                // we did not start processing CONNACK yet which means we haven't received it yet but the packet of different type has arrived.
                ShutdownOnError(context, new InvalidOperationException($"Invalid state: {this.stateFlags}, packet: {packet.PacketType}"));
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelRead));
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelReadComplete));

            base.ChannelReadComplete(context);
            if (this.InboundBacklogSize < this.mqttTransportSettings.MaxPendingInboundMessages)
            {
                context.Read();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ChannelReadComplete));
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ChannelInactive));
            
            if (this.mqttIotHubEventHandler.State == TransportState.Closed)
            {
                this.Shutdown(context);
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

        #endregion

        #region Connect
        async void Connect(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(Connect));
            
            try
            {
                string id = string.IsNullOrWhiteSpace(this.moduleId) ? this.deviceId : $"{this.deviceId}/{this.moduleId}";
                string password = null;
                if (this.passwordProvider != null)
                {
                    password = await this.passwordProvider.GetPasswordAsync().ConfigureAwait(false);
                }
                else
                {
                    Debug.Assert(this.mqttTransportSettings.ClientCertificate != null);
                }

                var connectPacket = new ConnectPacket
                {
                    ClientId = id,
                    HasUsername = true,
                    Username = $"{this.iotHubHostName}/{id}/?api-version={ClientApiVersionHelper.ApiVersionString}&DeviceClientType={Uri.EscapeDataString(this.productInfo.ToString())}",
                    HasPassword = !string.IsNullOrEmpty(password),
                    Password = password,
                    KeepAliveInSeconds = this.mqttTransportSettings.KeepAliveInSeconds,
                    CleanSession = this.mqttTransportSettings.CleanSession,
                    HasWill = this.mqttTransportSettings.HasWill
                };
                if (connectPacket.HasWill)
                {
                    Message message = this.willMessage.Message;
                    QualityOfService publishToServerQoS = this.mqttTransportSettings.PublishToServerQoS;
                    string topicName = this.GetTelemetryTopicName();
                    PublishPacket will = await Util.ComposePublishPacketAsync(context, message, publishToServerQoS, topicName).ConfigureAwait(false);

                    connectPacket.WillMessage = will.Payload;
                    connectPacket.WillQualityOfService = this.willMessage.QoS;
                    connectPacket.WillRetain = false;
                    connectPacket.WillTopicName = will.TopicName;
                }
                this.stateFlags = StateFlags.Connecting;

                await Util.WriteMessageAsync(context, connectPacket, ShutdownOnWriteErrorHandler).ConfigureAwait(false);
                this.lastChannelActivityTime = DateTime.UtcNow;
                this.ScheduleKeepConnectionAlive(context);

                this.ScheduleCheckConnectTimeout(context);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(Connect));
            }
        }

        async void ScheduleKeepConnectionAlive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ScheduleKeepConnectionAlive));
            
            try
            {
                await context.Channel.EventLoop.ScheduleAsync(PingServerCallback, context, this.pingRequestInterval).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ScheduleKeepConnectionAlive));
        }

        async void ScheduleCheckConnectTimeout(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(ScheduleCheckConnectTimeout));

            try
            {
                await context.Channel.EventLoop.ScheduleAsync(CheckConnAckTimeoutCallback, context, this.mqttTransportSettings.ConnectArrivalTimeout).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, nameof(ScheduleCheckConnectTimeout));
        }

        static void ShutdownIfNotReady(object state)
        {
            var context = (IChannelHandlerContext)state;
            var handler = (MqttIotHubAdapter)context.Handler;
            if (handler.IsInState(StateFlags.Connecting))
            {
                ShutdownOnError(context, new TimeoutException("Connection hasn't been established in time."));
            }
        }

        static async void PingServer(object ctx)
        {
            var context = (IChannelHandlerContext)ctx;
            try
            {
                var self = (MqttIotHubAdapter)context.Handler;

                if (!self.IsInState(StateFlags.Connected))
                {
                    return;
                }

                TimeSpan idleTime = DateTime.UtcNow - self.lastChannelActivityTime;

                if (idleTime > self.pingRequestInterval)
                {
                    // We've been idle for too long, send a ping!
                    await Util.WriteMessageAsync(context, PingReqPacket.Instance, ShutdownOnWriteErrorHandler).ConfigureAwait(false);
                }

                self.ScheduleKeepConnectionAlive(context);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
        }

        async Task ProcessConnectAckAsync(IChannelHandlerContext context, ConnAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessConnectAckAsync));
            
            if (packet.ReturnCode != ConnectReturnCode.Accepted)
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
                await this.SubscribeAsync(context, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessConnectAckAsync));
        }
        #endregion

        #region Subscribe

        async Task SubscribeAsync(IChannelHandlerContext context, SubscribePacket packetPassed)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetPassed, nameof(SubscribeAsync));
            
            string topicFilter;
            QualityOfService qos;

            if (packetPassed == null || packetPassed.Requests == null)
            {
                topicFilter = this.GetCommandTopicFilter();
                qos = mqttTransportSettings.ReceivingQoS;
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
                int packetId = Util.GetNextPacketId();
                var subscribePacket = new SubscribePacket(packetId, new SubscriptionRequest(topicFilter, qos));
                this.subscribeCompletions[packetId] = new TaskCompletionSource();

                await Util.WriteMessageAsync(context, subscribePacket, ShutdownOnWriteErrorHandler).ConfigureAwait(false);

                await this.subscribeCompletions[packetId].Task.ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetPassed, nameof(SubscribeAsync));
        }

        void ProcessSubAck(IChannelHandlerContext context, SubAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessSubAck));
            
            Contract.Assert(packet != null);

            TaskCompletionSource task;

            if (this.subscribeCompletions.TryRemove(packet.PacketId, out task))
            {
                task.TryComplete();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessSubAck));
        }

        #endregion

        #region Unsubscribe
        async Task UnSubscribeAsync(IChannelHandlerContext context, UnsubscribePacket packetPassed)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetPassed, nameof(UnSubscribeAsync));
            
            Contract.Assert(packetPassed != null);

            int packetId = Util.GetNextPacketId();
            packetPassed.PacketId = packetId;

            this.unsubscribeCompletions[packetId] = new TaskCompletionSource();

            await Util.WriteMessageAsync(context, packetPassed, ShutdownOnWriteErrorHandler).ConfigureAwait(false);

            await this.unsubscribeCompletions[packetId].Task.ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetPassed, nameof(UnSubscribeAsync));
        }

        void ProcessUnsubAck(IChannelHandlerContext context, UnsubAckPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessUnsubAck));
            
            Contract.Assert(packet != null);

            TaskCompletionSource task;
            if (this.unsubscribeCompletions.TryRemove(packet.PacketId, out task))
            {
                task.TryComplete();
            }

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessUnsubAck));
        }

        #endregion

        #region Receiving

        async void ProcessMessage(IChannelHandlerContext context, Packet packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet.PacketType, nameof(ProcessMessage));
            
            if (this.IsInState(StateFlags.Closed))
            {
                return;
            }

            try
            {
                switch (packet.PacketType)
                {
                    case PacketType.CONNACK:
                        await this.ProcessConnectAckAsync(context, (ConnAckPacket)packet).ConfigureAwait(false);
                        break;
                    case PacketType.SUBACK:
                        this.ProcessSubAck(context, packet as SubAckPacket);
                        break;
                    case PacketType.PUBLISH:
                        this.ProcessPublish(context, (PublishPacket)packet);
                        break;
                    case PacketType.PUBACK:
                        await this.serviceBoundTwoWayProcessor.CompleteWorkAsync(context, ((PubAckPacket)packet).PacketId).ConfigureAwait(false);
                        break;
                    case PacketType.PINGRESP:
                        break;
                    case PacketType.UNSUBACK:
                        this.ProcessUnsubAck(context, packet as UnsubAckPacket);
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

        Task AcceptMessageAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish, nameof(AcceptMessageAsync));
            
            Message message;
            try
            {
                var bodyStream = new ReadOnlyByteBufferStream(publish.Payload, true);

                message = new Message(bodyStream, true);

                Util.PopulateMessagePropertiesFromPacket(message, publish);

                message.MqttTopicName = publish.TopicName;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
                return TaskConstants.Completed;
            }

            this.mqttIotHubEventHandler.OnMessageReceived(message);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish, nameof(AcceptMessageAsync));

            return TaskConstants.Completed;
        }

        Task ProcessAckAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish?.Value, nameof(ProcessAckAsync));
            
            publish.Completion.Complete();

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish?.Value, nameof(ProcessAckAsync));

            return TaskConstants.Completed;
        }
#endregion

#region Sending
        void ProcessPublish(IChannelHandlerContext context, PublishPacket packet)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packet, nameof(ProcessPublish));
            
            if (this.IsInState(StateFlags.Closed))
            {
                return;
            }

            switch (packet.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    this.deviceBoundOneWayProcessor.Post(context, packet);
                    break;
                case QualityOfService.AtLeastOnce:
                    this.deviceBoundTwoWayProcessor.Post(context, packet);
                    break;
                default:
                    throw new NotSupportedException($"Unexpected QoS: '{packet.QualityOfService}'");
            }
            this.ResumeReadingIfNecessary(context);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, packet, nameof(ProcessPublish));
        }

        async Task SendMessageAsync(IChannelHandlerContext context, Message message)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, message, nameof(SendMessageAsync));

            string topicName;
            QualityOfService qos;

            if (string.IsNullOrEmpty(message.MqttTopicName))
            {
                topicName = this.GetTelemetryTopicName();
                qos = this.mqttTransportSettings.PublishToServerQoS;
            }
            else
            {
                topicName = message.MqttTopicName;
                qos = QualityOfService.AtMostOnce;
            }

            PublishPacket packet = await Util.ComposePublishPacketAsync(context, message, qos, topicName).ConfigureAwait(false);
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
                    this.serviceBoundOneWayProcessor.Post(context, workItem);
                    break;
                case QualityOfService.AtLeastOnce:
                    this.serviceBoundTwoWayProcessor.Post(context, workItem);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported telemetry QoS: '{this.mqttTransportSettings.PublishToServerQoS}'");
            }

            await publishCompletion.Task.ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, context.Name, message, nameof(SendMessageAsync));
        }

        Task AcknowledgeAsync(IChannelHandlerContext context, string packetIdString)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, packetIdString, nameof(AcknowledgeAsync));

            try
            {
                int packetId;
                if (int.TryParse(packetIdString, out packetId))
                {
                    return this.deviceBoundTwoWayProcessor.CompleteWorkAsync(context, packetId);
                }

                return TaskConstants.Completed;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, packetIdString, nameof(AcknowledgeAsync));
            }
        }

        Task SendAckAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish, nameof(SendAckAsync));

            try
            {
                this.ResumeReadingIfNecessary(context);
                return Util.WriteMessageAsync(context, PubAckPacket.InResponseTo(publish), ShutdownOnWriteErrorHandler);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish, nameof(SendAckAsync));
            }
        }

        async Task SendMessageToServerAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, publish?.Value, nameof(SendMessageToServerAsync));

            if (!this.IsInState(StateFlags.Connected))
            {
                publish.Completion.TrySetCanceled();
            }
            try
            {
                await Util.WriteMessageAsync(context, publish.Value, ShutdownOnWriteErrorHandler).ConfigureAwait(false);
                if (publish.Value.QualityOfService == QualityOfService.AtMostOnce)
                {
                    publish.Completion.TryComplete();
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Error(context.Handler, $"Context: {context.Name}: {ex.ToString()}", nameof(SendMessageToServerAsync));

                publish.Completion.TrySetException(ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, context.Name, publish?.Value, nameof(SendMessageToServerAsync));
            }
        }
#endregion

#region Shutdown
        static async void ShutdownOnError(IChannelHandlerContext context, Exception exception)
        {
            if (Logging.IsEnabled) Logging.Enter(context.Handler, context.Name, exception.ToString(), nameof(ShutdownOnError));
           
            var self = (MqttIotHubAdapter)context.Handler;
            if (!self.IsInState(StateFlags.Closed))
            {
                self.stateFlags |= StateFlags.Closed;
                foreach (var task in self.subscribeCompletions.Values)
                {
                    task.TrySetException(exception);
                }
                self.deviceBoundOneWayProcessor.Abort(exception);
                self.deviceBoundTwoWayProcessor.Abort(exception);
                self.serviceBoundOneWayProcessor.Abort(exception);
                self.serviceBoundTwoWayProcessor.Abort(exception);
                self.mqttIotHubEventHandler.OnError(exception);
                try
                {
                    await context.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    //ignored
                    if (Logging.IsEnabled) Logging.Error(context.Handler, $"Context: {context.Name}: {exception.ToString()}", nameof(ShutdownOnError));
                }
            }

            if (Logging.IsEnabled) Logging.Exit(context.Handler, context.Name, nameof(ShutdownOnError));
        }

        async void Shutdown(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled) Logging.Enter(this, context.Name, nameof(Shutdown));

            if (this.IsInState(StateFlags.Closed))
            {
                return;
            }

            try
            {
                this.stateFlags |= StateFlags.Closed; // "or" not to interfere with ongoing logic which has to honor Closed state when it's right time to do (case by case)

                this.CloseIotHubConnection();
                await context.CloseAsync().ConfigureAwait(false);
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

        async void CloseIotHubConnection()
        {
            if (Logging.IsEnabled) Logging.Enter(this, nameof(CloseIotHubConnection));
            
            if (this.IsInState(StateFlags.NotConnected) || this.IsInState(StateFlags.Connecting))
            {
                // closure has happened before IoT Hub connection was established or it was initiated due to disconnect
                return;
            }

            try
            {
                this.serviceBoundOneWayProcessor.Complete();
                this.deviceBoundOneWayProcessor.Complete();
                this.serviceBoundTwoWayProcessor.Complete();
                this.deviceBoundTwoWayProcessor.Complete();
                await Task.WhenAll(
                    this.serviceBoundOneWayProcessor.Completion,
                    this.serviceBoundTwoWayProcessor.Completion,
                    this.deviceBoundOneWayProcessor.Completion,
                    this.deviceBoundTwoWayProcessor.Completion).ConfigureAwait(false);
            }
            catch (Exception completeEx) when (!completeEx.IsFatal())
            {
                if (Logging.IsEnabled) Logging.Error(this, $"Complete exception: {completeEx.ToString()}", nameof(CloseIotHubConnection));
                
                try
                {
                    this.serviceBoundOneWayProcessor.Abort();
                    this.deviceBoundOneWayProcessor.Abort();
                    this.serviceBoundTwoWayProcessor.Abort();
                    this.deviceBoundTwoWayProcessor.Abort();
                }
                catch (Exception abortEx) when (!abortEx.IsFatal())
                {
                    // ignored on closing
                    if (Logging.IsEnabled) Logging.Error(this, $"Abort exception: {abortEx.ToString()}", nameof(CloseIotHubConnection));
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, nameof(CloseIotHubConnection));
            }
        }
#endregion

#region helper methods

        void ResumeReadingIfNecessary(IChannelHandlerContext context)
        {
            if (this.InboundBacklogSize == this.mqttTransportSettings.MaxPendingInboundMessages - 1) // we picked up a packet from full queue - now we have more room so order another read
            {
                context.Read();
            }
        }

        // TODO: directly use HasFlag
        bool IsInState(StateFlags stateFlagsToCheck)
        {
            return (this.stateFlags & stateFlagsToCheck) == stateFlagsToCheck;
        }

        IByteBuffer GetWillMessageBody(Message message)
        {
            Stream bodyStream = message.GetBodyStream();
            var buffer = new byte[bodyStream.Length];
            bodyStream.Read(buffer, 0, buffer.Length);
            IByteBuffer copiedBuffer = Unpooled.CopiedBuffer(buffer);
            return copiedBuffer;
        }

        string GetTelemetryTopicName()
        {
            string topicName = string.IsNullOrWhiteSpace(this.moduleId)
                ? DeviceTelemetryTopicFormat.FormatInvariant(this.deviceId)
                : ModuleTelemetryTopicFormat.FormatInvariant(this.deviceId, this.moduleId);
            return topicName;
        }

        string GetCommandTopicFilter()
        {
            string topicFilter = string.IsNullOrWhiteSpace(this.moduleId)
                ? DeviceCommandTopicFilterFormat.FormatInvariant(this.deviceId)
                : string.Empty;
            return topicFilter;
        }
#endregion
    }
}
