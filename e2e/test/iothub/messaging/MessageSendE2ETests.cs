// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
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

        // The size of a device to cloud message. This overly exceeds the maximum message size set by the hub, which is 256 KB. The reason why we are testing for this case is because
        // we noticed a different behavior between this case, and the case where the message size is less than 1 MB.
        private const int OverlyExceedAllowedMessageSizeInBytes = 3000 * 1024;

        private readonly string _devicePrefix = $"{nameof(MessageSendE2ETests)}_";
        private readonly string _modulePrefix = $"{nameof(MessageSendE2ETests)}_";
        private static string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_Amqp_WithHeartbeats()
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            amqpTransportSettings.IdleTimeout = TimeSpan.FromMinutes(2);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithHeartbeats()
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.IdleTimeout = TimeSpan.FromMinutes(2);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_Http_WithProxy()
        {
            var httpTransportSettings = new Client.Http1TransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);
            var transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task Message_DeviceSendSingleMessage_Http_WithCustomProxy()
        {
            var httpTransportSettings = new Http1TransportSettings();
            var proxy = new CustomWebProxy(Logger);
            httpTransportSettings.Proxy = proxy;
            var transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
            Assert.AreNotEqual(proxy.Counter, 0);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task Message_DeviceSendSingleMessage_MqttWs_WithProxy()
        {
            var mqttTransportSettings =
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task Message_ModuleSendSingleMessage_AmqpWs_WithProxy()
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task Message_ModuleSendSingleMessage_MqttWs_WithProxy()
        {
            var mqttTransportSettings =
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendBatchMessages_Amqp()
        {
            await SendBatchMessages(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendBatchMessages_AmqpWs()
        {
            await SendBatchMessages(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_DeviceSendBatchMessages_Mqtt()
        {
            await SendBatchMessages(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendBatchMessages_MqttWs()
        {
            await SendBatchMessages(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendBatchMessages_Http()
        {
            await SendBatchMessages(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_ClientThrowsForMqttTopicNameTooLong()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var msg = new Client.Message(Encoding.UTF8.GetBytes("testMessage"));
            //Mqtt topic name consists of, among other things, system properties and user properties
            // setting lots of very long user properties should cause a MessageTooLargeException explaining
            // that the topic name is too long to publish over mqtt
            for (int i = 0; i < 100; i++)
            {
                msg.Properties.Add(Guid.NewGuid().ToString(), new string('1', 1024));
            }

            await deviceClient.SendEventAsync(msg).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Http1, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Http1, LargeMessageSizeInBytes)]
        public async Task Message_DeviceSendSingleLargeMessageAsync(TestDeviceType testDeviceType, Client.TransportType transportType, int messageSize)
        {
            await SendSingleMessage(testDeviceType, transportType, messageSize).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_Amqp()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, ExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, ExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, ExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, ExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageOverAllowedSize_Http()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, ExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_Amqp()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, OverlyExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, OverlyExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, OverlyExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        // MQTT protocol will throw an InvalidOperationException if the PUBLISH packet is greater than
        // Hub limits: https://github.com/Azure/azure-iot-sdk-csharp/blob/d46e0f07fe8d80e21e07b41c2e75b0bd1fcb8f80/iothub/device/src/Transport/Mqtt/MqttIotHubAdapter.cs#L1175
        // This flow is a bit different from other protocols where we do not inspect the packet being sent but rather rely on service validating it
        // and throwing a MessageTooLargeException, if relevant.
        [LoggedTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, OverlyExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_DeviceSendMessageWayOverAllowedSize_Http()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, OverlyExceedAllowedMessageSizeInBytes).ConfigureAwait(false);
        }

        private async Task SendSingleMessage(TestDeviceType type, Client.TransportType transport, int messageSize = 0)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, testDevice.Id, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendBatchMessages(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendBatchMessagesAsync(deviceClient, testDevice.Id, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendSingleMessage(TestDeviceType type, ITransportSettings[] transportSettings, int messageSize = 0)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, testDevice.Id, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendSingleMessageModule(ITransportSettings[] transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings);

            await moduleClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageModuleAsync(moduleClient, testModule.DeviceId).ConfigureAwait(false);
            await moduleClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task SendSingleMessageAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger, int messageSize = 0)
        {
            Client.Message testMessage;

            if (messageSize == 0)
            {
                (testMessage, _, _) = ComposeD2cTestMessage(logger);
            }
            else
            {
                (testMessage, _, _) = ComposeD2cTestMessageOfSpecifiedSize(messageSize, logger);
            }

            using (testMessage)
            {
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
            }
        }

        public static async Task SendBatchMessagesAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger)
        {
            var messagesToBeSent = new Dictionary<Client.Message, Tuple<string, string>>();

            try
            {
                var props = new List<Tuple<string, string>>();
                for (int i = 0; i < MessageBatchCount; i++)
                {
                    (Client.Message testMessage, string payload, string p1Value) = ComposeD2cTestMessage(logger);
                    messagesToBeSent.Add(testMessage, Tuple.Create(payload, p1Value));
                }

                await deviceClient.SendEventBatchAsync(messagesToBeSent.Keys.ToList()).ConfigureAwait(false);
            }
            finally
            {
                foreach (KeyValuePair<Client.Message, Tuple<string, string>> messageEntry in messagesToBeSent)
                {
                    Client.Message message = messageEntry.Key;
                    message.Dispose();
                }
            }
        }

        private async Task SendSingleMessageModuleAsync(ModuleClient moduleClient, string deviceId)
        {
            (Client.Message testMessage, string payload, string p1Value) = ComposeD2cTestMessage(Logger);

            using (testMessage)
            {
                await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
            }
        }

        public static (Client.Message message, string payload, string p1Value) ComposeD2cTestMessage(MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeD2cTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return (message, payload, p1Value);
        }

        public static (Client.Message message, string payload, string p1Value) ComposeD2cTestMessageOfSpecifiedSize(int messageSize, MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = $"{Guid.NewGuid()}_{new string('*', messageSize)}";
            string p1Value = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeD2cTestMessageOfSpecifiedSize)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return (message, payload, p1Value);
        }
    }
}
