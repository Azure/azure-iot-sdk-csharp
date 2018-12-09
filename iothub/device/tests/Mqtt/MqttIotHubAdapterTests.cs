namespace Microsoft.Azure.Devices.Client.Test.Mqtt
{
    using DotNetty.Codecs.Mqtt.Packets;
    using DotNetty.Transport.Channels;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Threading.Tasks;

    [TestClass]
    [TestCategory("Unit")]
    public class MqttIotHubAdapterTest
    {
        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_SubscribeContinuesBeforeSubAckReceived()
        {
            var request = new SubscribePacket(0, new SubscriptionRequest("myTopic", QualityOfService.AtMostOnce));
            Func<PacketWithId, SubAckPacket> ackFactory =
                (packet) =>
                {
                    return new SubAckPacket() { PacketId = packet.PacketId };
                };

            await SendRequestAndAcknowledgementsInSpecificOrder(request, ackFactory, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_SubscribeContinuesAfterUnsubAckReceived()
        {
            var request = new SubscribePacket(0, new SubscriptionRequest("myTopic", QualityOfService.AtMostOnce));
            Func<PacketWithId, SubAckPacket> ackFactory =
                (packet) => new SubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcknowledgementsInSpecificOrder(request, ackFactory, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_UnsubscribeContinuesBeforeUnsubAckReceived()
        {
            var request = new UnsubscribePacket(0, "myTopic");
            Func<PacketWithId, UnsubAckPacket> ackFactory =
                (packet) => new UnsubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcknowledgementsInSpecificOrder(request, ackFactory, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_UnsubscribeContinuesAfterUnsubAckReceived()
        {
            var request = new UnsubscribePacket(0, "myTopic");
            Func<PacketWithId, UnsubAckPacket> ackFactory =
                (packet) => new UnsubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcknowledgementsInSpecificOrder(request, ackFactory, true).ConfigureAwait(false);
        }

        private async Task SendRequestAndAcknowledgementsInSpecificOrder<T>(T requestPacket, Func<T, PacketWithId> ackFactory, bool receiveResponseBeforeSendingRequestContinues)
        {
            var passwordProvider = new Mock<IAuthorizationProvider>();
            var mqttIotHubEventHandler = new Mock<IMqttIotHubEventHandler>();
            var productInfo = new ProductInfo();
            var mqttTransportSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) { HasWill = false };
            var channelHandlerContext = new Mock<IChannelHandlerContext>();

            var mqttIotHubAdapter = new MqttIotHubAdapter("deviceId", string.Empty, string.Empty, passwordProvider.Object, mqttTransportSetting, null, mqttIotHubEventHandler.Object, productInfo);

            // Setup internal state to be "Connected". Only then can we manage subscriptions
            // "NotConnected" -> (ChannelActive) -> "Connecting" -> (ChannelRead ConnAck) -> "Connected".
            channelHandlerContext.Setup(context => context.Channel.EventLoop.ScheduleAsync(It.IsAny<Action>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            channelHandlerContext.SetupGet(context => context.Handler).Returns(mqttIotHubAdapter);
            mqttIotHubAdapter.ChannelActive(channelHandlerContext.Object);
            mqttIotHubAdapter.ChannelRead(channelHandlerContext.Object, new ConnAckPacket() { ReturnCode = ConnectReturnCode.Accepted, SessionPresent = false });

            // Setup sending of a Packet so that the matching response is received before sending task is completed.
            var startRequest = new TaskCompletionSource<T>();
            Action sendResponse = async () =>
            {
                var response = ackFactory(await startRequest.Task.ConfigureAwait(false));
                mqttIotHubAdapter.ChannelRead(channelHandlerContext.Object, response);
            };

            channelHandlerContext.Setup(context => context.WriteAndFlushAsync(It.IsAny<T>()))
            .Callback<object>((packet) => startRequest.SetResult((T)packet))
            .Returns(receiveResponseBeforeSendingRequestContinues ? Task.Run(sendResponse) : Task.CompletedTask);

            // Act:
            // Send the request (and response if not done as mocked "sending" task) packets
            var sendRequest = mqttIotHubAdapter.WriteAsync(channelHandlerContext.Object, requestPacket);
            if(!receiveResponseBeforeSendingRequestContinues)
            {
                sendResponse();
            }

            // Assert: No matter the event ordering, sending should be awaitable without errors
            await sendRequest.ConfigureAwait(false);
        }
    }
}