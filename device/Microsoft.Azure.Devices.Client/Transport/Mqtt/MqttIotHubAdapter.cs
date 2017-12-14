﻿// Copyright (c) Microsoft. All rights reserved.
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
#if !WINDOWS_UWP
    using DotNetty.Handlers.Tls;
#endif
    using DotNetty.Transport.Channels;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using System.Diagnostics;

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
        const string CommandTopicFilterFormat = "devices/{0}/messages/devicebound/#";
        const string TelemetryTopicFormat = "devices/{0}/messages/events/";


        static readonly Action<object> PingServerCallback = PingServer;
        static readonly Action<object> CheckConnAckTimeoutCallback = ShutdownIfNotReady;
        static readonly Func<IChannelHandlerContext, Exception, bool> ShutdownOnWriteErrorHandler = (ctx, ex) => { ShutdownOnError(ctx, ex); return false; };

        readonly IMqttIotHubEventHandler mqttIotHubEventHandler;

        readonly string deviceId;
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
            this.stateFlags = StateFlags.NotConnected;

            this.Connect(context);

            // TODO #223: this executes in parallel with the Connect(context).

            base.ChannelActive(context);

            context.Read();
        }

        public override async Task WriteAsync(IChannelHandlerContext context, object data)
        {
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
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
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
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            base.ChannelReadComplete(context);
            if (this.InboundBacklogSize < this.mqttTransportSettings.MaxPendingInboundMessages)
            {
                context.Read();
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (this.mqttIotHubEventHandler.State == TransportState.Closed)
            {
                this.Shutdown(context);
                base.ChannelInactive(context);
            }
            else
            {
                ShutdownOnError(context, new SocketException());
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            ShutdownOnError(context, exception);
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object @event)
        {
#if WINDOWS_UWP
            throw new NotImplementedException("Not supported in UWP");
#else
            var handshakeCompletionEvent = @event as TlsHandshakeCompletionEvent;
            if (handshakeCompletionEvent != null && !handshakeCompletionEvent.IsSuccessful)
            {
                ShutdownOnError(context, handshakeCompletionEvent.Exception);
            }
#endif
        }

#endregion

#region Connect
        async void Connect(IChannelHandlerContext context)
        {
            try
            {
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
                    ClientId = this.deviceId,
                    HasUsername = true,
                    Username = $"{this.iotHubHostName}/{this.deviceId}/api-version={ClientApiVersionHelper.ApiVersionString}&DeviceClientType={Uri.EscapeDataString(this.productInfo.ToString())}",
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
                    string topicName = string.Format(TelemetryTopicFormat, this.deviceId);
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
        }

        async void ScheduleKeepConnectionAlive(IChannelHandlerContext context)
        {
            try
            {
                await context.Channel.EventLoop.ScheduleAsync(PingServerCallback, context, this.pingRequestInterval).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
        }

        async void ScheduleCheckConnectTimeout(IChannelHandlerContext context)
        {
            try
            {
                await context.Channel.EventLoop.ScheduleAsync(CheckConnAckTimeoutCallback, context, this.mqttTransportSettings.ConnectArrivalTimeout).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                ShutdownOnError(context, ex);
            }
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
        }
#endregion

#region Subscribe

        async Task SubscribeAsync(IChannelHandlerContext context, SubscribePacket packetPassed)
        {
            string topicFilter;
            QualityOfService qos;

            if (packetPassed == null || packetPassed.Requests == null)
            {
                topicFilter = CommandTopicFilterFormat.FormatInvariant(this.deviceId);
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


            int packetId = Util.GetNextPacketId();
            var subscribePacket = new SubscribePacket(packetId, new SubscriptionRequest(topicFilter, qos));
            this.subscribeCompletions[packetId] = new TaskCompletionSource();

            await Util.WriteMessageAsync(context, subscribePacket, ShutdownOnWriteErrorHandler).ConfigureAwait(false);

            await this.subscribeCompletions[packetId].Task.ConfigureAwait(false);

        }

        void ProcessSubAck(SubAckPacket packet)
        {
            Contract.Assert(packet != null);

            TaskCompletionSource task;

            if (this.subscribeCompletions.TryRemove(packet.PacketId, out task))
            {
                task.TryComplete();
            }
        }

#endregion

#region Unsubscribe
        async Task UnSubscribeAsync(IChannelHandlerContext context, UnsubscribePacket packetPassed)
        {
            Contract.Assert(packetPassed != null);

            int packetId = Util.GetNextPacketId();
            packetPassed.PacketId = packetId;

            this.unsubscribeCompletions[packetId] = new TaskCompletionSource();

            await Util.WriteMessageAsync(context, packetPassed, ShutdownOnWriteErrorHandler).ConfigureAwait(false);

            await this.unsubscribeCompletions[packetId].Task.ConfigureAwait(false);
        }

        void ProcessUnsubAck(UnsubAckPacket packet)
        {
            Contract.Assert(packet != null);

            TaskCompletionSource task;
            if (this.unsubscribeCompletions.TryRemove(packet.PacketId, out task))
            {
                task.TryComplete();
            }
        }

#endregion

#region Receiving

        async void ProcessMessage(IChannelHandlerContext context, Packet packet)
        {
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
                        this.ProcessSubAck(packet as SubAckPacket);
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
                        this.ProcessUnsubAck(packet as UnsubAckPacket);
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
        }

        Task AcceptMessageAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            Message message;
            try
            {
                var bodyStream = new ReadOnlyByteBufferStream(publish.Payload, true);

                message = new Message(bodyStream, true);

                Util.PopulateMessagePropertiesFromPacket(message, publish);

                message.MqttTopicName = publish.TopicName;
            }
            catch (Exception ex)
            {
                ShutdownOnError(context, ex);
                return TaskConstants.Completed;
            }
            this.mqttIotHubEventHandler.OnMessageReceived(message);
            return TaskConstants.Completed;
        }

        Task ProcessAckAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
            publish.Completion.Complete();
            return TaskConstants.Completed;
        }
#endregion

#region Sending
        void ProcessPublish(IChannelHandlerContext context, PublishPacket packet)
        {
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
        }

        async Task SendMessageAsync(IChannelHandlerContext context, Message message)
        {
            string topicName;
            QualityOfService qos;
                
            if (string.IsNullOrEmpty(message.MqttTopicName))
            {
                topicName = string.Format(TelemetryTopicFormat, this.deviceId);
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
        }

        Task AcknowledgeAsync(IChannelHandlerContext context, string packetIdString)
        {
            int packetId;
            if (int.TryParse(packetIdString, out packetId))
            {
                return this.deviceBoundTwoWayProcessor.CompleteWorkAsync(context, packetId);
            }
            return TaskConstants.Completed;
        }

        Task SendAckAsync(IChannelHandlerContext context, PublishPacket publish)
        {
            this.ResumeReadingIfNecessary(context);
            return Util.WriteMessageAsync(context, PubAckPacket.InResponseTo(publish), ShutdownOnWriteErrorHandler);
        }

        async Task SendMessageToServerAsync(IChannelHandlerContext context, PublishWorkItem publish)
        {
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
                publish.Completion.TrySetException(ex);
            }
        }
#endregion

#region Shutdown
        static async void ShutdownOnError(IChannelHandlerContext context, Exception exception)
        {
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
                }
            }
        }

        async void Shutdown(IChannelHandlerContext context)
        {
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
            }
        }

        async void CloseIotHubConnection()
        {
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
                }
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
#endregion
    }
}
