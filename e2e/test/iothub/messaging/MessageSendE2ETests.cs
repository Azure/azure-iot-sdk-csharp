// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public partial class MessageSendE2ETests : E2EMsTestBase
    {
        private const int MessageBatchCount = 5;

        // The maximum message size for device to cloud messages is 256 KB. We are allowing 1 KB of buffer for message header information etc.
        private const int LargeMessageSizeInBytes = 255 * 1024;

        // The size of a device to cloud message. This exceeds the the maximum message size set by the hub; 256 KB.
        private const int ExceedAllowedMessageSizeInBytes = 300 * 1024;

        // The size of a device to cloud message. This overly exceeds the maximum message size set by the hub, which is 256 KB.
        // The reason why we are testing for this case is because we noticed a different behavior between this case, and the case where
        // the message size is less than 1 MB.
        private const int OverlyExceedAllowedMessageSizeInBytes = 3000 * 1024;

        private readonly string _devicePrefix = $"{nameof(MessageSendE2ETests)}_";
        private readonly string _modulePrefix = $"{nameof(MessageSendE2ETests)}_";
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.Sasl, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.Sasl, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Amqp_WithHeartbeats(IotHubClientTransportProtocol protocol)
        {
            var amqpTransportSettings = new IotHubClientAmqpSettings(protocol)
            {
                IdleTimeout = TimeSpan.FromMinutes(2),
            };
            await SendSingleMessage(TestDeviceType.Sasl, amqpTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            var amqpTransportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)
            {
                Proxy = new WebProxy(s_proxyServerAddress),
            };

            await SendSingleMessage(TestDeviceType.Sasl, amqpTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.X509, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.X509, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendBatchMessages_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendBatchMessages(TestDeviceType.X509, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendBatchMessages_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendBatchMessages(TestDeviceType.X509, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_ClientThrowsForMqttTopicNameTooLong()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);

            try
            {
                using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
                await deviceClient.OpenAsync().ConfigureAwait(false);

                var msg = new Client.Message(Encoding.UTF8.GetBytes("testMessage"));
                //Mqtt topic name consists of, among other things, system properties and user properties
                // setting lots of very long user properties should cause a MessageTooLargeException explaining
                // that the topic name is too long to publish over mqtt
                for (int i = 0; i < 100; i++)
                {
                    msg.Properties.Add(Guid.NewGuid().ToString(), new string('1', 1024));
                }

                // act
                Func<Task> act = async () =>
                {
                    await deviceClient.SendEventAsync(msg).ConfigureAwait(false);
                };

                // assert
                var error = await act.Should().ThrowAsync<IotHubClientException>();
                error.And.StatusCode.Should().Be(IotHubStatusCode.MessageTooLarge);
                error.And.IsTransient.Should().BeFalse();
            }
            finally
            {
                await testDevice.RemoveDeviceAsync();
            }
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, TestDeviceType.Sasl)]
        [DataRow(IotHubClientTransportProtocol.Tcp, TestDeviceType.X509)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, TestDeviceType.Sasl)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, TestDeviceType.X509)]
        public async Task Message_DeviceSendSingleLargeMessageAsync_Amqp(IotHubClientTransportProtocol protocol, TestDeviceType testDeviceType)
        {
            var transportSettings = new IotHubClientAmqpSettings(protocol);
            await Message_DeviceSendSingleLargeMessageAsync(testDeviceType, transportSettings);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, TestDeviceType.Sasl)]
        [DataRow(IotHubClientTransportProtocol.Tcp, TestDeviceType.X509)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, TestDeviceType.Sasl)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, TestDeviceType.X509)]
        public async Task Message_DeviceSendSingleLargeMessageAsync_Mqtt(IotHubClientTransportProtocol protocol, TestDeviceType testDeviceType)
        {
            var transportSettings = new IotHubClientMqttSettings(protocol);
            await Message_DeviceSendSingleLargeMessageAsync(testDeviceType, transportSettings);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendMessageOverAllowedSize_Amqp(IotHubClientTransportProtocol protocol)
        {
            // act
            Func<Task> act = async () =>
            {
                await SendSingleMessage(
                        TestDeviceType.Sasl,
                        new IotHubClientAmqpSettings(protocol),
                        ExceedAllowedMessageSizeInBytes)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.MessageTooLarge);
            error.And.IsTransient.Should().BeFalse();
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(protocol),
                    ExceedAllowedMessageSizeInBytes)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_Amqp(IotHubClientTransportProtocol protocol)
        {
            // act
            Func<Task> act = async () =>
            {
                await SendSingleMessage(
                        TestDeviceType.Sasl,
                        new IotHubClientAmqpSettings(protocol),
                        OverlyExceedAllowedMessageSizeInBytes)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.MessageTooLarge);
            error.And.IsTransient.Should().BeFalse();
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(InvalidOperationException))]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(protocol),
                    OverlyExceedAllowedMessageSizeInBytes)
                .ConfigureAwait(false);
        }

        private async Task Message_DeviceSendSingleLargeMessageAsync(TestDeviceType testDeviceType, IotHubClientTransportSettings transportSettings)
        {
            await SendSingleMessage(testDeviceType, transportSettings, LargeMessageSizeInBytes).ConfigureAwait(false);
        }

        private async Task SendSingleMessage(TestDeviceType type, IotHubClientTransportSettings transportSettings, int messageSize = 0)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendBatchMessages(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendBatchMessagesAsync(deviceClient, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendSingleMessageModule(IotHubClientTransportSettings transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);

            await moduleClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageModuleAsync(moduleClient).ConfigureAwait(false);
            await moduleClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task SendSingleMessageAsync(IotHubDeviceClient deviceClient, MsTestLogger logger, int messageSize = 0)
        {
            Client.Message testMessage = messageSize == 0
                ? ComposeD2cTestMessage(logger, out string _, out string _)
                : ComposeD2cTestMessageOfSpecifiedSize(messageSize, logger, out string _, out string _);

            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
        }

        public static async Task SendBatchMessagesAsync(IotHubDeviceClient deviceClient, MsTestLogger logger)
        {
            var messagesToBeSent = new Dictionary<Client.Message, Tuple<string, string>>();

            for (int i = 0; i < MessageBatchCount; i++)
            {
                Client.Message testMessage = ComposeD2cTestMessage(logger, out string payload, out string p1Value);
                messagesToBeSent.Add(testMessage, Tuple.Create(payload, p1Value));
            }

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.SendEventBatchAsync(messagesToBeSent.Keys.ToList()).ConfigureAwait(false);
        }

        private async Task SendSingleMessageModuleAsync(IotHubModuleClient moduleClient)
        {
            Client.Message testMessage = ComposeD2cTestMessage(Logger, out string _, out string _);

            await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
        }

        public static Client.Message ComposeD2cTestMessage(MsTestLogger logger, out string payload, out string p1Value)
        {
            string messageId = Guid.NewGuid().ToString();
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeD2cTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return message;
        }

        public static Client.Message ComposeD2cTestMessageOfSpecifiedSize(int messageSize, MsTestLogger logger, out string payload, out string p1Value)
        {
            string messageId = Guid.NewGuid().ToString();
            payload = $"{Guid.NewGuid()}_{new string('*', messageSize)}";
            p1Value = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeD2cTestMessageOfSpecifiedSize)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return message;
        }
    }
}
