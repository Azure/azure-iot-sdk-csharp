// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class SecurityMessageSendE2ETests : IDisposable
    {
        private static readonly string DevicePrefix = $"E2E_{nameof(SecurityMessageSendE2ETests)}_";
        private static readonly string ModulePrefix = $"E2E_{nameof(SecurityMessageSendE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public SecurityMessageSendE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Amqp()
        {
            await SendSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Mqtt()
        {
            await SendSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_MqttWs()
        {
            await SendSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Http()
        {
            await SendSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Amqp()
        {
            await SendSecurityMessageModule(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_AmqpWs()
        {
            await SendSecurityMessageModule(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Mqtt()
        {
            await SendSecurityMessageModule(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_MqttWs()
        {
            await SendSecurityMessageModule(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        private static async Task SendSecurityMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            string deviceId = testDevice.Id;
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                (Client.Message testMessage, string messageId, string payload, string p1Value) = ComposeTestSecurityMessage();
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(deviceId, payload, p1Value);
                Assert.IsFalse(isReceived, "Secured message should not be received.");

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task SendSecurityMessageModule(Client.TransportType transport)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            string deviceId = testModule.Id;
            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transport))
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);
                (Client.Message testMessage, string messageId, string payload, string p1Value) = ComposeTestSecurityMessage();
                await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(deviceId, payload, p1Value);
                Assert.IsFalse(isReceived, "Secured essage should not be received.");
                
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static (Client.Message message, string messageId, string payload, string p1Value) ComposeTestSecurityMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var payload = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeTestSecurityMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };
            message.SetAsSecurityMessage();
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
