// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public partial class MessageSendE2ETests : IDisposable
    {
        private const int MESSAGE_BATCH_COUNT = 5;
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private static readonly string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static readonly TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageSendE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Http1)]
        public async Task Message_DeviceSendSingleMessage(Client.TransportType transportType)
        {
            await SendSingleMessage(TestDeviceType.Sasl, transportType).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Message_DeviceSendSingleMessage_WithHeartbeats(Client.TransportType transportType)
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(transportType)
            {
                IdleTimeout = TimeSpan.FromMinutes(2),
            };
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

#if !NETCOREAPP1_1

        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_Http_WithProxy()
        {
            var httpTransportSettings = new Http1TransportSettings
            {
                Proxy = new WebProxy(ProxyServerAddress),
            };
            var transportSettings = new ITransportSettings[] { httpTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only)
            {
                Proxy = new WebProxy(ProxyServerAddress),
            };
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Message_DeviceSendSingleMessage_MqttWs_WithProxy()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only)
            {
                Proxy = new WebProxy(ProxyServerAddress),
            };
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Message_ModuleSendSingleMessage_AmqpWs_WithProxy()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only)
            {
                Proxy = new WebProxy(ProxyServerAddress),
            };
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessageModule(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Message_ModuleSendSingleMessage_MqttWs_WithProxy()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only)
            {
                Proxy = new WebProxy(ProxyServerAddress),
            };
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };
            await SendSingleMessageModule(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Http1)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task X509_DeviceSendSingleMessage(Client.TransportType transportType)
        {
            await SendSingleMessage(TestDeviceType.X509, transportType).ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Http1)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task X509_DeviceSendBatchMessages(Client.TransportType transportType)
        {
            await SendBatchMessages(TestDeviceType.X509, transportType).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(MessageTooLargeException))]
        public async Task Message_ClientThrowsForMqttTopicNameTooLong()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                var msg = new Client.Message(Encoding.UTF8.GetBytes("testMessage"));
                //Mqtt topic name consists of, among other things, system properties and user properties
                // setting lots of very long user properties should cause a MessageTooLargeException explaining
                // that the topic name is too long to publish over mqtt
                for (int i = 0; i < 100; i++)
                {
                    msg.Properties.Add(Guid.NewGuid().ToString(), new string('1', 1024));
                }

                await deviceClient.SendEventAsync(msg).ConfigureAwait(false);
            }
        }

#endif

        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Message_DeviceSendSingleMessage_Http_WithCustomProxy()
        {
            var proxy = new CustomWebProxy();
            var httpTransportSettings = new Http1TransportSettings
            {
                Proxy = proxy,
            };
            var transportSettings = new ITransportSettings[] { httpTransportSettings };
            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
            Assert.AreNotEqual(0, proxy.Counter);
        }

        private async Task SendSingleMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendBatchMessages(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await SendSendBatchMessagesAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendSingleMessage(TestDeviceType type, ITransportSettings[] transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendSingleMessageModule(TestDeviceType type, ITransportSettings[] transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            using (var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings))
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageModuleAndVerifyAsync(moduleClient, testModule.DeviceId).ConfigureAwait(false);
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task SendSingleMessageAndVerifyAsync(DeviceClient deviceClient, string deviceId)
        {
            TestMessage d2cMessage = ComposeD2CTestMessage();
            await deviceClient.SendEventAsync(d2cMessage.ClientMessage).ConfigureAwait(false);

            bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(deviceId, d2cMessage.Payload, d2cMessage.P1Value);
            Assert.IsTrue(isReceived, "Message is not received.");
        }

        public static async Task SendSendBatchMessagesAndVerifyAsync(DeviceClient deviceClient, string deviceId)
        {
            var messages = new List<Client.Message>();
            var props = new List<Tuple<string, string>>();
            for (int i = 0; i < MESSAGE_BATCH_COUNT; i++)
            {
                TestMessage d2cMessage = ComposeD2CTestMessage();
                messages.Add(d2cMessage.ClientMessage);
                props.Add(Tuple.Create(d2cMessage.Payload, d2cMessage.P1Value));
            }

            await deviceClient.SendEventBatchAsync(messages).ConfigureAwait(false);

            foreach (Tuple<string, string> prop in props)
            {
                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(deviceId, prop.Item1, prop.Item2);
                Assert.IsTrue(isReceived, "Message is not received.");
            }
        }

        private async Task SendSingleMessageModuleAndVerifyAsync(ModuleClient moduleClient, string deviceId)
        {
            TestMessage d2cMessage = ComposeD2CTestMessage();
            await moduleClient.SendEventAsync(d2cMessage.ClientMessage).ConfigureAwait(false);

            bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(deviceId, d2cMessage.Payload, d2cMessage.P1Value);
            Assert.IsTrue(isReceived, "Message is not received.");
        }

        public static TestMessage ComposeD2CTestMessage()
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return new TestMessage
            {
                ClientMessage = message,
                Payload = payload,
                P1Value = p1Value,
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
