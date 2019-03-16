// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class MessageSendE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(MessageSendE2ETests)}_";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
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

            await SendSingleMessageModule(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_ModuleSendSingleMessage_MqttWs_WithProxy()
        {
            Client.Transport.Mqtt.MqttTransportSettings mqttTransportSettings = 
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessageModule(TestDeviceType.Sasl, transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
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

        private async Task FastTimeout()
        {
            TimeSpan? timeout = TimeSpan.FromTicks(1);
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            TimeSpan? timeout = null;
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task TestTimeout(TimeSpan? timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Amqp))
            {
                await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), timeout).ConfigureAwait(false);
            }
        }

        private Client.Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: payload='{payload}' p1Value='{p1Value}'");

            return new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
        }

        private Message ComposeC2DTestMessage(out string payload, out string messageId, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            messageId = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeC2DTestMessage)}: payload='{payload}' messageId='{messageId}' p1Value='{p1Value}'");

            return new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };
        }

        private async Task SendSingleMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await SendSingleMessage(deviceClient, testDevice.Id).ConfigureAwait(false);
            }
        }

        private async Task SendSingleMessage(TestDeviceType type, ITransportSettings[] transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportSettings))
            {
                await SendSingleMessage(deviceClient, testDevice.Id).ConfigureAwait(false);
            }
        }
        
        private async Task SendSingleMessage(DeviceClient deviceClient, string deviceId)
        {
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(deviceId).ConfigureAwait(false);

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                string payload;
                string p1Value;
                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = await testListener.WaitForMessage(deviceId, payload, p1Value).ConfigureAwait(false);
                Assert.IsTrue(isReceived, "Message is not received.");
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await testListener.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendSingleMessageModule(TestDeviceType type, ITransportSettings[] transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings))
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);

                string payload;
                string p1Value;
                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
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
