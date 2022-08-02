// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.Mqtt
{
    using DotNetty.Codecs.Mqtt.Packets;
    using DotNetty.Transport.Channels;
    using FluentAssertions;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    [TestCategory("Unit")]
    public class MqttIotHubAdapterTest
    {
        [TestMethod]
        public void TestPopulateMessagePropertiesFromPacket_NormalMessage()
        {
            var message = new Message();
            var publishPacket = new PublishPacket(QualityOfService.AtMostOnce, false, false)
            {
                PacketId = 0,
                TopicName = "devices/d10/messages/devicebound/%24.cid=Corrid1&%24.mid=MessageId1&Prop1=Value1&Prop2=Value2&Prop3=Value3/"
            };

            MqttIotHubAdapter.PopulateMessagePropertiesFromPacket(message, publishPacket);
            Assert.AreEqual(3, message.Properties.Count);
            Assert.AreEqual("Value1", message.Properties["Prop1"]);
            Assert.AreEqual("Value2", message.Properties["Prop2"]);
            Assert.AreEqual("Value3", message.Properties["Prop3"]);

            Assert.AreEqual(3, message.SystemProperties.Count);
            Assert.AreEqual("Corrid1", message.SystemProperties["correlation-id"]);
            Assert.AreEqual("MessageId1", message.SystemProperties["message-id"]);
        }

        [TestMethod]
        public void TestPopulateMessagePropertiesFromPacket_ModuleEndpointMessage()
        {
            var message = new Message();
            var publishPacket = new PublishPacket(QualityOfService.AtMostOnce, false, false)
            {
                PacketId = 0,
                TopicName = "devices/d10/modules/m3/endpoints/in2/%24.cid=Corrid1&%24.mid=MessageId1&Prop1=Value1&Prop2=Value2&Prop3=Value3/"
            };

            MqttIotHubAdapter.PopulateMessagePropertiesFromPacket(message, publishPacket);
            Assert.AreEqual(3, message.Properties.Count);
            Assert.AreEqual("Value1", message.Properties["Prop1"]);
            Assert.AreEqual("Value2", message.Properties["Prop2"]);
            Assert.AreEqual("Value3", message.Properties["Prop3"]);

            Assert.AreEqual(3, message.SystemProperties.Count);
            Assert.AreEqual("Corrid1", message.SystemProperties["correlation-id"]);
            Assert.AreEqual("MessageId1", message.SystemProperties["message-id"]);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_SubscribeContinuesBeforeSubAckReceived()
        {
            var request = new SubscribePacket(0, new SubscriptionRequest("myTopic", QualityOfService.AtMostOnce));
            Func<PacketWithId, SubAckPacket> ackFactory =
                (packet) =>
                {
                    return new SubAckPacket() { PacketId = packet.PacketId };
                };

            await SendRequestAndAcksInSpecificOrderAsync(request, ackFactory, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_SubscribeContinuesAfterUnsubAckReceived()
        {
            var request = new SubscribePacket(0, new SubscriptionRequest("myTopic", QualityOfService.AtMostOnce));
            Func<PacketWithId, SubAckPacket> ackFactory =
                (packet) => new SubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcksInSpecificOrderAsync(request, ackFactory, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_UnsubscribeContinuesBeforeUnsubAckReceived()
        {
            var request = new UnsubscribePacket(0, "myTopic");
            Func<PacketWithId, UnsubAckPacket> ackFactory =
                (packet) => new UnsubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcksInSpecificOrderAsync(request, ackFactory, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldNotAssumeEventOrdering_UnsubscribeContinuesAfterUnsubAckReceived()
        {
            var request = new UnsubscribePacket(0, "myTopic");
            Func<PacketWithId, UnsubAckPacket> ackFactory =
                (packet) => new UnsubAckPacket() { PacketId = packet.PacketId };

            await SendRequestAndAcksInSpecificOrderAsync(request, ackFactory, true).ConfigureAwait(false);
        }

        [TestMethod]
        public void TestAuthenticationChain()
        {
            const string authChain = "leaf;edge1;edge2";
            var passwordProvider = new Mock<IAuthorizationProvider>();
            var mqttIotHubEventHandler = new Mock<IMqttIotHubEventHandler>();
            var productInfo = new ProductInfo();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            var mqttTransportSetting = new IotHubClientMqttSettings { HasWill = false };
            var channelHandlerContext = new Mock<IChannelHandlerContext>();
            var mqttIotHubAdapter = new MqttIotHubAdapter("deviceId", string.Empty, string.Empty, passwordProvider.Object, mqttTransportSetting, null, mqttIotHubEventHandler.Object, productInfo, options);

            // Set an authchain on the transport settings
            mqttTransportSetting.AuthenticationChain = authChain;

            // Save all the messages from the context
            List<object> messages = new List<object>();
            channelHandlerContext.Setup(context => context.WriteAndFlushAsync(It.IsAny<object>())).Callback((object message) => messages.Add(message)).Returns(TaskHelpers.CompletedTask);

            // Act
            channelHandlerContext.Setup(context => context.Channel.EventLoop.ScheduleAsync(It.IsAny<Action>(), It.IsAny<TimeSpan>())).Returns(TaskHelpers.CompletedTask);
            channelHandlerContext.SetupGet(context => context.Handler).Returns(mqttIotHubAdapter);
            mqttIotHubAdapter.ChannelActive(channelHandlerContext.Object);

            // Assert: the auth chain should be part of the username
            ConnectPacket connectPacket = messages.First().As<ConnectPacket>();
            NameValueCollection queryParams = ExtractQueryParamsFromConnectUsername(connectPacket.Username);
            Assert.AreEqual(authChain, queryParams.Get("auth-chain"));
        }

        private async Task SendRequestAndAcksInSpecificOrderAsync<T>(T requestPacket, Func<T, PacketWithId> ackFactory, bool receiveResponseBeforeSendingRequestContinues)
        {
            var passwordProvider = new Mock<IAuthorizationProvider>();
            var mqttIotHubEventHandler = new Mock<IMqttIotHubEventHandler>();
            var productInfo = new ProductInfo();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            var mqttTransportSetting = new IotHubClientMqttSettings { HasWill = false };
            var channelHandlerContext = new Mock<IChannelHandlerContext>();

            var mqttIotHubAdapter = new MqttIotHubAdapter(
                "deviceId",
                string.Empty,
                string.Empty,
                passwordProvider.Object,
                mqttTransportSetting,
                null,
                mqttIotHubEventHandler.Object,
                productInfo,
                options);

            // Setup internal state to be "Connected". Only then can we manage subscriptions
            // "NotConnected" -> (ChannelActive) -> "Connecting" -> (ChannelRead ConnAck) -> "Connected".
            channelHandlerContext
                .Setup(context => context.Channel.EventLoop.ScheduleAsync(It.IsAny<Action>(), It.IsAny<TimeSpan>()))
                .Returns(TaskHelpers.CompletedTask);
            channelHandlerContext.SetupGet(context => context.Handler).Returns(mqttIotHubAdapter);
            mqttIotHubAdapter.ChannelActive(channelHandlerContext.Object);
            mqttIotHubAdapter.ChannelRead(channelHandlerContext.Object, new ConnAckPacket { ReturnCode = ConnectReturnCode.Accepted, SessionPresent = false });

            // Setup sending of a Packet so that the matching response is received before sending task is completed.
            var startRequest = new TaskCompletionSource<T>();
            Action sendResponse = async () =>
            {
                var response = ackFactory(await startRequest.Task.ConfigureAwait(false));
                mqttIotHubAdapter.ChannelRead(channelHandlerContext.Object, response);
            };

            channelHandlerContext.Setup(context => context.WriteAndFlushAsync(It.IsAny<T>()))
                .Callback<object>((packet) => startRequest.SetResult((T)packet))
                .Returns(receiveResponseBeforeSendingRequestContinues
                    ? Task.Run(sendResponse)
                    : TaskHelpers.CompletedTask);

            // Act:
            // Send the request (and response if not done as mocked "sending" task) packets
            var sendRequest = mqttIotHubAdapter.WriteAsync(channelHandlerContext.Object, requestPacket);
            if (!receiveResponseBeforeSendingRequestContinues)
            {
                sendResponse();
            }

            // Assert: No matter the event ordering, sending should be awaitable without errors
            await sendRequest.ConfigureAwait(false);
        }

        [TestMethod]
        public void SettingPnpModelIdShouldSetModelIdOnConnectPacket()
        {
            // arrange
            string ModelIdParam = "model-id";
            var passwordProvider = new Mock<IAuthorizationProvider>();
            var mqttIotHubEventHandler = new Mock<IMqttIotHubEventHandler>();
            var mqttTransportSetting = new IotHubClientMqttSettings();
            var productInfo = new ProductInfo();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { ModelId = "someModel" };
            var channelHandlerContext = new Mock<IChannelHandlerContext>();
            var mqttIotHubAdapter = new MqttIotHubAdapter("deviceId", string.Empty, string.Empty, passwordProvider.Object, mqttTransportSetting, null, mqttIotHubEventHandler.Object, productInfo, options);

            // Save all the messages from the context
            var messages = new List<object>();
            channelHandlerContext.Setup(context => context.WriteAndFlushAsync(It.IsAny<object>())).Callback((object message) => messages.Add(message)).Returns(TaskHelpers.CompletedTask);

            // Act
            channelHandlerContext.SetupGet(context => context.Handler).Returns(mqttIotHubAdapter);
            mqttIotHubAdapter.ChannelActive(channelHandlerContext.Object);

            // Assert: the username should use the preview API version and have the model ID appended
            ConnectPacket connectPacket = messages.First().As<ConnectPacket>();
            NameValueCollection queryParams = ExtractQueryParamsFromConnectUsername(connectPacket.Username);
            Assert.AreEqual(options.ModelId, queryParams.Get(ModelIdParam));
        }

        [TestMethod]
        public void NotSettingPnpModelIdShouldNotSetModelIdOnConnectPacket()
        {
            // arrange
            string ModelIdParam = "model-id";
            var passwordProvider = new Mock<IAuthorizationProvider>();
            var mqttIotHubEventHandler = new Mock<IMqttIotHubEventHandler>();
            var mqttTransportSetting = new IotHubClientMqttSettings();
            var productInfo = new ProductInfo();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            var channelHandlerContext = new Mock<IChannelHandlerContext>();
            var mqttIotHubAdapter = new MqttIotHubAdapter("deviceId", string.Empty, string.Empty, passwordProvider.Object, mqttTransportSetting, null, mqttIotHubEventHandler.Object, productInfo, options);

            // Save all the messages from the context
            var messages = new List<object>();
            channelHandlerContext.Setup(context => context.WriteAndFlushAsync(It.IsAny<object>())).Callback((object message) => messages.Add(message)).Returns(TaskHelpers.CompletedTask);

            // Act
            channelHandlerContext.SetupGet(context => context.Handler).Returns(mqttIotHubAdapter);
            mqttIotHubAdapter.ChannelActive(channelHandlerContext.Object);

            // Assert: the username should use the GA API version and not have the model ID appended
            ConnectPacket connectPacket = messages.First().As<ConnectPacket>();
            NameValueCollection queryParams = ExtractQueryParamsFromConnectUsername(connectPacket.Username);
            Assert.IsFalse(queryParams.AllKeys.Contains(ModelIdParam));
        }

        private static NameValueCollection ExtractQueryParamsFromConnectUsername(string connectUsername)
        {
            int pos = connectUsername.LastIndexOf("?") + 1;
            string queryParams = connectUsername.Substring(pos, connectUsername.Length - pos);
            return System.Web.HttpUtility.ParseQueryString(queryParams);
        }
    }
}
