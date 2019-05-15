// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class MessageSendE2ETests : IDisposable
    {
        private enum MessageType
        {
            Regular,
            Security
        }

        private readonly static string DevicePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private readonly static string ModulePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private readonly static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageSendE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_Http_WithProxy()
        {
            Client.Http1TransportSettings httpTransportSettings = new Client.Http1TransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_Http_WithCustomeProxy()
        {
            Http1TransportSettings httpTransportSettings = new Http1TransportSettings();
            CustomWebProxy proxy = new CustomWebProxy();
            httpTransportSettings.Proxy = proxy;
            ITransportSettings[] transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
            Assert.AreNotEqual(proxy.Counter, 0);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            Client.AmqpTransportSettings amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_MqttWs_WithProxy()
        {
            Client.Transport.Mqtt.MqttTransportSettings mqttTransportSettings =
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessage(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_ModuleSendSingleMessage_AmqpWs_WithProxy()
        {
            Client.AmqpTransportSettings amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_ModuleSendSingleMessage_MqttWs_WithProxy()
        {
            Client.Transport.Mqtt.MqttTransportSettings mqttTransportSettings =
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Amqp()
        {
            await SendSingleMessageModule(Client.TransportType.Amqp_Tcp_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_AmqpWs()
        {
            await SendSingleMessageModule(Client.TransportType.Amqp_WebSocket_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Mqtt()
        {
            await SendSingleMessageModule(Client.TransportType.Mqtt_Tcp_Only, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_MqttWs()
        {
            await SendSingleMessageModule(Client.TransportType.Mqtt_WebSocket_Only, true).ConfigureAwait(false);
        }

        private static async Task SendSingleMessage(TestDeviceType type, Client.TransportType transport, bool secured = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id, secured).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task SendSingleMessage(TestDeviceType type, ITransportSettings[] transportSettings, bool secured = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id, secured).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task SendSingleMessageModule(Client.TransportType transport, bool secured = false)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transport))
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageModuleAndVerifyAsync(moduleClient, testModule.DeviceId, secured).ConfigureAwait(false);
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task SendSingleMessageModule(ITransportSettings[] transportSettings, bool secured = false)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings))
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);
                await SendSingleMessageModuleAndVerifyAsync(moduleClient, testModule.DeviceId, secured).ConfigureAwait(false);
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task SendSingleMessageAndVerifyAsync(DeviceClient deviceClient, string deviceId, bool secured = false)
        {
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(deviceId).ConfigureAwait(false);

            try
            {
                (Client.Message testMessage, string messageId, string payload, string p1Value) = ComposeD2CTestMessage();
                if (secured)
                {
                    testMessage.SetAsSecurityMessage();
                }

                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
                bool isReceived = await testListener.WaitForMessage(deviceId, payload, p1Value).ConfigureAwait(false);

                if (secured)
                {
                    Assert.IsFalse(isReceived, "Secured essage should not be received.");
                }
                else
                {
                    Assert.IsTrue(isReceived, "Message is not received.");
                }
            }
            finally
            {
                await testListener.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task SendSingleMessageModuleAndVerifyAsync(ModuleClient moduleClient, string deviceId, bool secured = false)
        {
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(deviceId).ConfigureAwait(false);

            try
            {
                (Client.Message testMessage, string messageId, string payload, string p1Value) = ComposeD2CTestMessage();
                if (secured)
                {
                    testMessage.SetAsSecurityMessage();
                }

                await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = await testListener.WaitForMessage(deviceId, payload, p1Value).ConfigureAwait(false);

                if (secured)
                {
                    Assert.IsFalse(isReceived, "Secured essage should not be received.");
                }
                else
                {
                    Assert.IsTrue(isReceived, "Message is not received.");
                }
            }
            finally
            {
                await testListener.CloseAsync().ConfigureAwait(false);
            }
        }

        public static (Client.Message message, string messageId, string payload, string p1Value) ComposeD2CTestMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var payload = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
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
