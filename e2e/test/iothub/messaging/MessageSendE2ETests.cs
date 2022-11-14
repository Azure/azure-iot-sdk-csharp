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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.Sasl, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.Sasl, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
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

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.X509, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendSingleMessage(TestDeviceType.X509, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendBatchMessages_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendBatchMessages(TestDeviceType.X509, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_DeviceSendBatchMessages_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendBatchMessages(TestDeviceType.X509, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceOpenCloseOpenSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            await OpenCloseOpenThenSendSingleMessage(TestDeviceType.Sasl, new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceOpenCloseOpenSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await OpenCloseOpenThenSendSingleMessage(TestDeviceType.Sasl, new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        private async Task Message_DeviceSendSingleLargeMessageAsync(TestDeviceType testDeviceType, IotHubClientTransportSettings transportSettings)
        {
            await SendSingleMessage(testDeviceType, transportSettings, LargeMessageSizeInBytes).ConfigureAwait(false);
        }

        private async Task SendSingleMessage(TestDeviceType type, IotHubClientTransportSettings transportSettings, int messageSize = 0)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, messageSize).ConfigureAwait(false);
        }

        private async Task SendBatchMessages(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendBatchMessagesAsync(deviceClient).ConfigureAwait(false);
        }

        private async Task SendSingleMessageModule(IotHubClientTransportSettings transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);

            await moduleClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageModuleAsync(moduleClient).ConfigureAwait(false);
        }

        public static async Task SendSingleMessageAsync(IotHubDeviceClient deviceClient, int messageSize = 0)
        {
            TelemetryMessage testMessage = messageSize == 0
                ? ComposeD2cTestMessage(out string _, out string _)
                : ComposeD2cTestMessageOfSpecifiedSize(messageSize, out string _, out string _);

            await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
        }

        public static async Task SendBatchMessagesAsync(IotHubDeviceClient deviceClient)
        {
            var messagesToBeSent = new Dictionary<TelemetryMessage, Tuple<string, string>>();

            for (int i = 0; i < MessageBatchCount; i++)
            {
                TelemetryMessage testMessage = ComposeD2cTestMessage(out string payload, out string p1Value);
                messagesToBeSent.Add(testMessage, Tuple.Create(payload, p1Value));
            }

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.SendTelemetryBatchAsync(messagesToBeSent.Keys.ToList()).ConfigureAwait(false);
        }

        private async Task SendSingleMessageModuleAsync(IotHubModuleClient moduleClient)
        {
            TelemetryMessage testMessage = ComposeD2cTestMessage(out string _, out string _);

            await moduleClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
        }

        private async Task OpenCloseOpenThenSendSingleMessage(TestDeviceType type, IotHubClientTransportSettings transportSettings, int messageSize = 0)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // The re-opened client under test should still be able to send telemetry.
            await SendSingleMessageAsync(deviceClient, messageSize).ConfigureAwait(false);
        }

        public static TelemetryMessage ComposeD2cTestMessage(out string payload, out string p1Value)
        {
            string messageId = Guid.NewGuid().ToString();
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeD2cTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new TelemetryMessage(payload)
            {
                MessageId = messageId,
                UserId = userId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return message;
        }

        public static TelemetryMessage ComposeD2cTestMessageOfSpecifiedSize(int messageSize, out string payload, out string p1Value)
        {
            string messageId = Guid.NewGuid().ToString();
            payload = $"{Guid.NewGuid()}_{new string('*', messageSize)}";
            p1Value = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeD2cTestMessageOfSpecifiedSize)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new TelemetryMessage(payload)
            {
                MessageId = messageId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return message;
        }
    }
}
