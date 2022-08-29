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
    using FluentAssertions;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    [DoNotParallelize]
    public class ClientWebSocketChannelTests
    {
        private const string IotHubName = "localhost";
        private const int Port = 12346;
        private const string ClientId = "scenarioClient1";
        private const string PublishS2CQos1Payload = "S->C, QoS 1 test. Different data length #2.";
        private const string PublishS2CQos1Topic = "test2/scenarioClient1/special/qos/One";

        private static HttpListener s_listener;
        private static ServerWebSocketChannel s_serverWebSocketChannel;
        private static ReadListeningHandler s_serverListener;
        private static volatile bool s_isDone;

        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(300);

        [ClassInitialize]
        public static void ClassInitialize(TestContext testcontext)
        {
            s_listener = new HttpListener();
            s_listener.Prefixes.Add("http://+:" + Port + WebSocketConstants.UriSuffix + "/");
            s_listener.Start();

            _ = RunWebSocketServer();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_isDone = true;
            s_listener.Stop();
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
        [TestMethod]
        public async Task ClientWebSocketChannelReadAfterCloseTest()
        {
            using var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            var uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);

            var clientReadListener = new ReadListeningHandler();
            using var clientChannel = new ClientWebSocketChannel(null, websocket);
            clientChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default);

            clientChannel.Pipeline.AddLast(clientReadListener);
            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientChannel).ConfigureAwait(false);
            await clientChannel.CloseAsync().ConfigureAwait(false);

            // Test read API
            try
            {
                await clientReadListener.ReceiveAsync(s_defaultTimeout).ConfigureAwait(false);
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException e)
            {
                e.Message.Contains("Channel is closed").Should().BeTrue();
            }
        }

        [TestMethod]
        public async Task ClientWebSocketChannelWriteAfterCloseTest()
        {
            using var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            var uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketChannel = new ClientWebSocketChannel(null, websocket);

            var threadLoop = new SingleThreadEventLoop("MQTTExecutionThread", TimeSpan.FromSeconds(1));
            await threadLoop.RegisterAsync(clientWebSocketChannel).ConfigureAwait(false);
            await clientWebSocketChannel.CloseAsync().ConfigureAwait(false);

            // Test write API
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
                innerException.Should().NotBeNull();
            }
        }

        [TestMethod]
        public async Task MqttWebSocketClientAndServerScenario()
        {
            using var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);
            var uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
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

            await Task
                .WhenAll(
                    RunMqttClientScenarioAsync(clientChannel, clientReadListener),
                    RunMqttServerScenarioAsync(s_serverWebSocketChannel, s_serverListener))
                .ConfigureAwait(false);
        }

        private static async Task RunMqttClientScenarioAsync(IChannel channel, ReadListeningHandler readListener)
        {
            const string SubscribeTopicFilter1 = "test/+";
            const string SubscribeTopicFilter2 = "test2/#";
            const string PublishC2STopic = "loopback/qosZero";
            const string PublishC2SQos0Payload = "C->S, QoS 0 test.";
            const string PublishC2SQos1Topic = "loopback2/qos/One";
            const string PublishC2SQos1Payload = "C->S, QoS 1 test. Different data length.";

            await channel
                .WriteAndFlushAsync(
                    new ConnectPacket
                    {
                        ClientId = ClientId,
                        Username = "testuser",
                        Password = "notsafe",
                        WillTopicName = "last/word",
                        WillMessage = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("oops"))
                    })
                .ConfigureAwait(false);

            var connAckPacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as ConnAckPacket;
            connAckPacket.Should().NotBeNull();
            connAckPacket.ReturnCode.Should().Be(ConnectReturnCode.Accepted);

            int subscribePacketId = GetRandomPacketId();
            int unsubscribePacketId = GetRandomPacketId();
            await channel.WriteAndFlushManyAsync(
                new SubscribePacket(subscribePacketId,
                    new SubscriptionRequest(SubscribeTopicFilter1, QualityOfService.ExactlyOnce),
                    new SubscriptionRequest(SubscribeTopicFilter2, QualityOfService.AtLeastOnce),
                    new SubscriptionRequest("for/unsubscribe", QualityOfService.AtMostOnce)),
                new UnsubscribePacket(unsubscribePacketId, "for/unsubscribe")).ConfigureAwait(false);

            var subAckPacket = await readListener.ReceiveAsync(s_defaultTimeout).ConfigureAwait(false) as SubAckPacket;
            subAckPacket.Should().NotBeNull();
            subAckPacket.PacketId.Should().Be(subscribePacketId);
            subAckPacket.ReturnCodes.Count.Should().Be(3);
            subAckPacket.ReturnCodes[0].Should().Be(QualityOfService.ExactlyOnce);
            subAckPacket.ReturnCodes[1].Should().Be(QualityOfService.AtLeastOnce);
            subAckPacket.ReturnCodes[2].Should().Be(QualityOfService.AtMostOnce);

            var unsubAckPacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as UnsubAckPacket;
            unsubAckPacket.Should().NotBeNull();
            unsubAckPacket.PacketId.Should().Be(unsubscribePacketId);

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

            var pubAckPacket = await readListener.ReceiveAsync(s_defaultTimeout).ConfigureAwait(false) as PubAckPacket;
            pubAckPacket.Should().NotBeNull();
            pubAckPacket.PacketId.Should().Be(publishQoS1PacketId);

            var publishPacket = await readListener.ReceiveAsync(s_defaultTimeout).ConfigureAwait(false) as PublishPacket;
            publishPacket.Should().NotBeNull();
            publishPacket.QualityOfService.Should().Be(QualityOfService.AtLeastOnce);
            publishPacket.TopicName.Should().Be(PublishS2CQos1Topic);
            publishPacket.Payload.ToString(Encoding.UTF8).Should().Be(PublishS2CQos1Payload);

            await channel
                .WriteAndFlushManyAsync(
                    PubAckPacket.InResponseTo(publishPacket),
                    DisconnectPacket.Instance)
                .ConfigureAwait(false);
        }

        private static async Task RunMqttServerScenarioAsync(IChannel channel, ReadListeningHandler readListener)
        {
            var connectPacket = await readListener.ReceiveAsync(s_defaultTimeout).ConfigureAwait(false) as ConnectPacket;
            Assert.IsNotNull(connectPacket, "Must be a Connect pkt");
            // todo verify

            await channel
                .WriteAndFlushAsync(
                    new ConnAckPacket
                    {
                        ReturnCode = ConnectReturnCode.Accepted,
                        SessionPresent = true,
                    })
                .ConfigureAwait(false);

            var subscribePacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as SubscribePacket;
            subscribePacket.Should().NotBeNull();
            // todo verify

            await channel
                .WriteAndFlushAsync(SubAckPacket.InResponseTo(subscribePacket, QualityOfService.ExactlyOnce))
                .ConfigureAwait(false);

            var unsubscribePacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as UnsubscribePacket;
            unsubscribePacket.Should().NotBeNull();
            // todo verify

            await channel
                .WriteAndFlushAsync(UnsubAckPacket.InResponseTo(unsubscribePacket))
                .ConfigureAwait(false);

            var publishQos0Packet = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as PublishPacket;
            publishQos0Packet.Should().NotBeNull();
            // todo verify

            var publishQos1Packet = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as PublishPacket;
            publishQos1Packet.Should().NotBeNull();
            // todo verify

            int publishQos1PacketId = GetRandomPacketId();
            await channel
                .WriteAndFlushManyAsync(
                    PubAckPacket.InResponseTo(publishQos1Packet),
                    new PublishPacket(QualityOfService.AtLeastOnce, false, false)
                    {
                        PacketId = publishQos1PacketId,
                        TopicName = PublishS2CQos1Topic,
                        Payload = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(PublishS2CQos1Payload)),
                    })
                .ConfigureAwait(false);

            var pubAckPacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as PubAckPacket;
            pubAckPacket.PacketId.Should().Be(publishQos1PacketId);

            var disconnectPacket = await readListener
                .ReceiveAsync(s_defaultTimeout)
                .ConfigureAwait(false) as DisconnectPacket;
            disconnectPacket.Should().NotBeNull();
        }

        private static int GetRandomPacketId() => Guid.NewGuid().GetHashCode() & ushort.MaxValue;

        private static async Task RunWebSocketServer()
        {
            try
            {
                while (!s_isDone)
                {
                    HttpListenerContext context = await s_listener.GetContextAsync().ConfigureAwait(false);
                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }

                    HttpListenerWebSocketContext webSocketContext = await context
                        .AcceptWebSocketAsync(WebSocketConstants.SubProtocols.Mqtt, 8 * 1024, TimeSpan.FromMinutes(5))
                        .ConfigureAwait(false);

                    s_serverListener = new ReadListeningHandler();
                    s_serverWebSocketChannel = new ServerWebSocketChannel(null, webSocketContext.WebSocket, context.Request.RemoteEndPoint);
                    s_serverWebSocketChannel
                        .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                        .Option(ChannelOption.AutoRead, true)
                        .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                        .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default);
                    s_serverWebSocketChannel.Pipeline.AddLast("server logger", new LoggingHandler("SERVER"));
                    s_serverWebSocketChannel.Pipeline.AddLast(
                        MqttEncoder.Instance,
                        new MqttDecoder(true, 256 * 1024),
                        s_serverListener);
                    var workerGroup = new MultithreadEventLoopGroup();
                    await workerGroup.RegisterAsync(s_serverWebSocketChannel).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (WebSocketException)
            {
                _ = RunWebSocketServer();
            }
            catch (HttpListenerException)
            {
                return;
            }
        }
    }
}
