// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.Mqtt
{
    using System;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Mqtt;
    using DotNetty.Codecs.Mqtt.Packets;
    using DotNetty.Handlers.Logging;
    using DotNetty.Transport.Channels;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ClientWebSocketChannelTests
    {
        private const string IotHubName = "localhost";
        private const int Port = 12346;
        private static HttpListener listener;
        private static ServerWebSocketChannel serverWebSocketChannel;
        private static ReadListeningHandler serverListener;
        private static volatile bool done;

        private const string ClientId = "scenarioClient1";
        private const string SubscribeTopicFilter1 = "test/+";
        private const string SubscribeTopicFilter2 = "test2/#";
        private const string PublishC2STopic = "loopback/qosZero";
        private const string PublishC2SQos0Payload = "C->S, QoS 0 test.";
        private const string PublishC2SQos1Topic = "loopback2/qos/One";
        private const string PublishC2SQos1Payload = "C->S, QoS 1 test. Different data length.";
        private const string PublishS2CQos1Topic = "test2/scenarioClient1/special/qos/One";
        private const string PublishS2CQos1Payload = "S->C, QoS 1 test. Different data length #2.";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(300);

        [ClassInitialize()]
        public static void AssembyInitialize(TestContext testcontext)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://+:" + Port + WebSocketConstants.UriSuffix + "/");
            listener.Start();

            RunWebSocketServerAsync().ContinueWith(t => t, TaskContinuationOptions.OnlyOnFaulted);
        }

        [ClassCleanup()]
        public static void AssemblyCleanup()
        {
            listener.Stop();
        }

        [ExpectedException(typeof(ClosedChannelException))]
        [TestMethod]
        public async Task ClientWebSocketChannelWriteWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            using var clientWebSocketChannel = new ClientWebSocketChannel(null, websocket);
            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientWebSocketChannel).ConfigureAwait(false);
            await clientWebSocketChannel.WriteAndFlushAsync(new ConnectPacket()).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ClosedChannelException))]
        [TestMethod]
        public async Task ClientWebSocketChannelReadWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            using var clientWebSocketChannel = new ClientWebSocketChannel(null, websocket);
            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientWebSocketChannel).ConfigureAwait(false);
            clientWebSocketChannel.Read();
        }

        // The following tests can only be run in Administrator mode
        [Ignore] //TODO #318
        [TestMethod]
        public async Task ClientWebSocketChannelReadAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            var uri = new Uri("ws://" + IotHubName + ":" + Port + WebSocketConstants.UriSuffix);
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);

            var clientReadListener = new ReadListeningHandler();
            using var clientChannel = new ClientWebSocketChannel(null, websocket);
            clientChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default);

            clientChannel.Pipeline.AddLast(
                clientReadListener);
            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientChannel).ConfigureAwait(false);
            await clientChannel.CloseAsync().ConfigureAwait(false);

            // Test Read API
            try
            {
                await clientReadListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false);
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException e)
            {
                Assert.IsTrue(e.Message.Contains("Channel is closed"));
            }

            done = true;
        }

        [TestMethod]
        [Ignore] //TODO #318
        public async Task ClientWebSocketChannelWriteAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            var uri = new Uri("ws://" + IotHubName + ":" + Port + WebSocketConstants.UriSuffix);
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketChannel = new ClientWebSocketChannel(null, websocket);

            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientWebSocketChannel).ConfigureAwait(false);
            await clientWebSocketChannel.CloseAsync().ConfigureAwait(false);

            // Test Write API
            try
            {
                await clientWebSocketChannel.WriteAndFlushAsync(Unpooled.Buffer()).ConfigureAwait(false);
                Assert.Fail("Should have thrown ClosedChannelException");
            }
            catch (ClosedChannelException)
            {
            }
            catch (AggregateException e)
            {
                var innerException = e.InnerException as ClosedChannelException;
                Assert.IsNotNull(innerException);
            }

            done = true;
        }

        [TestMethod]
        [Ignore] //TODO #318
        public async Task MqttWebSocketClientAndServerScenario()
        {
            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            Uri uri = new Uri("ws://" + IotHubName + ":" + Port + WebSocketConstants.UriSuffix);
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);

            var clientReadListener = new ReadListeningHandler();
            using var clientChannel = new ClientWebSocketChannel(null, websocket);
            clientChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default);

            clientChannel.Pipeline.AddLast(
                MqttEncoder.Instance,
                new MqttDecoder(false, 256 * 1024),
                clientReadListener);
            var clientWorkerGroup = new MultithreadEventLoopGroup();
            await clientWorkerGroup.RegisterAsync(clientChannel).ConfigureAwait(false);

            await Task.WhenAll(RunMqttClientScenarioAsync(clientChannel, clientReadListener), RunMqttServerScenarioAsync(serverWebSocketChannel, serverListener)).ConfigureAwait(false);
            done = true;
        }

        private static async Task RunMqttClientScenarioAsync(IChannel channel, ReadListeningHandler readListener)
        {
            await channel.WriteAndFlushAsync(new ConnectPacket
            {
                ClientId = ClientId,
                Username = "testuser",
                Password = "notsafe",
                WillTopicName = "last/word",
                WillMessage = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("oops"))
            }).ConfigureAwait(false);

            var connAckPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as ConnAckPacket;
            Assert.IsNotNull(connAckPacket);
            Assert.AreEqual(ConnectReturnCode.Accepted, connAckPacket.ReturnCode);

            int subscribePacketId = GetRandomPacketId();
            int unsubscribePacketId = GetRandomPacketId();
            await channel.WriteAndFlushManyAsync(
                new SubscribePacket(subscribePacketId,
                    new SubscriptionRequest(SubscribeTopicFilter1, QualityOfService.ExactlyOnce),
                    new SubscriptionRequest(SubscribeTopicFilter2, QualityOfService.AtLeastOnce),
                    new SubscriptionRequest("for/unsubscribe", QualityOfService.AtMostOnce)),
                new UnsubscribePacket(unsubscribePacketId, "for/unsubscribe")).ConfigureAwait(false);

            var subAckPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as SubAckPacket;
            Assert.IsNotNull(subAckPacket);
            Assert.AreEqual(subscribePacketId, subAckPacket.PacketId);
            Assert.AreEqual(3, subAckPacket.ReturnCodes.Count);
            Assert.AreEqual(QualityOfService.ExactlyOnce, subAckPacket.ReturnCodes[0]);
            Assert.AreEqual(QualityOfService.AtLeastOnce, subAckPacket.ReturnCodes[1]);
            Assert.AreEqual(QualityOfService.AtMostOnce, subAckPacket.ReturnCodes[2]);

            var unsubAckPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as UnsubAckPacket;
            Assert.IsNotNull(unsubAckPacket);
            Assert.AreEqual(unsubscribePacketId, unsubAckPacket.PacketId);

            int publishQoS1PacketId = GetRandomPacketId();
            await channel.WriteAndFlushManyAsync(
                new PublishPacket(QualityOfService.AtMostOnce, false, false)
                {
                    TopicName = PublishC2STopic,
                    Payload = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(PublishC2SQos0Payload))
                },
                new PublishPacket(QualityOfService.AtLeastOnce, false, false)
                {
                    PacketId = publishQoS1PacketId,
                    TopicName = PublishC2SQos1Topic,
                    Payload = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(PublishC2SQos1Payload))
                }).ConfigureAwait(false);
            //new PublishPacket(QualityOfService.AtLeastOnce, false, false) { TopicName = "feedback/qos/One", Payload = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("QoS 1 test. Different data length.")) });

            var pubAckPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as PubAckPacket;
            Assert.IsNotNull(pubAckPacket);
            Assert.AreEqual(publishQoS1PacketId, pubAckPacket.PacketId);

            var publishPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as PublishPacket;
            Assert.IsNotNull(publishPacket);
            Assert.AreEqual(QualityOfService.AtLeastOnce, publishPacket.QualityOfService);
            Assert.AreEqual(PublishS2CQos1Topic, publishPacket.TopicName);
            Assert.AreEqual(PublishS2CQos1Payload, publishPacket.Payload.ToString(Encoding.UTF8));

            await channel.WriteAndFlushManyAsync(
                PubAckPacket.InResponseTo(publishPacket),
                DisconnectPacket.Instance).ConfigureAwait(false);
        }

        private static async Task RunMqttServerScenarioAsync(IChannel channel, ReadListeningHandler readListener)
        {
            var connectPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as ConnectPacket;
            Assert.IsNotNull(connectPacket, "Must be a Connect pkt");
            // todo verify

            await channel.WriteAndFlushAsync(new ConnAckPacket
            {
                ReturnCode = ConnectReturnCode.Accepted,
                SessionPresent = true
            }).ConfigureAwait(false);

            var subscribePacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as SubscribePacket;
            Assert.IsNotNull(subscribePacket);
            // todo verify

            await channel.WriteAndFlushAsync(SubAckPacket.InResponseTo(subscribePacket, QualityOfService.ExactlyOnce)).ConfigureAwait(false);

            var unsubscribePacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as UnsubscribePacket;
            Assert.IsNotNull(unsubscribePacket);
            // todo verify

            await channel.WriteAndFlushAsync(UnsubAckPacket.InResponseTo(unsubscribePacket)).ConfigureAwait(false);

            var publishQos0Packet = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as PublishPacket;
            Assert.IsNotNull(publishQos0Packet);
            // todo verify

            var publishQos1Packet = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as PublishPacket;
            Assert.IsNotNull(publishQos1Packet);
            // todo verify

            int publishQos1PacketId = GetRandomPacketId();
            await channel.WriteAndFlushManyAsync(
                PubAckPacket.InResponseTo(publishQos1Packet),
                new PublishPacket(QualityOfService.AtLeastOnce, false, false)
                {
                    PacketId = publishQos1PacketId,
                    TopicName = PublishS2CQos1Topic,
                    Payload = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(PublishS2CQos1Payload))
                }).ConfigureAwait(false);

            var pubAckPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as PubAckPacket;
            Assert.AreEqual(publishQos1PacketId, pubAckPacket.PacketId);

            var disconnectPacket = await readListener.ReceiveAsync(DefaultTimeout).ConfigureAwait(false) as DisconnectPacket;
            Assert.IsNotNull(disconnectPacket);
        }

        private static int GetRandomPacketId() => Guid.NewGuid().GetHashCode() & ushort.MaxValue;

        private static async Task RunWebSocketServerAsync()
        {
            HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }

            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(WebSocketConstants.SubProtocols.Mqtt, 8 * 1024, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

            serverListener = new ReadListeningHandler();
            serverWebSocketChannel = new ServerWebSocketChannel(null, webSocketContext.WebSocket, context.Request.RemoteEndPoint);
            serverWebSocketChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default);
            serverWebSocketChannel.Pipeline.AddLast("server logger", new LoggingHandler("SERVER"));
            serverWebSocketChannel.Pipeline.AddLast(
                MqttEncoder.Instance,
                new MqttDecoder(true, 256 * 1024),
                serverListener);
            var workerGroup = new MultithreadEventLoopGroup();
            await workerGroup.RegisterAsync(serverWebSocketChannel).ConfigureAwait(false);

            while (true)
            {
                if (done)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
        }
    }
}
