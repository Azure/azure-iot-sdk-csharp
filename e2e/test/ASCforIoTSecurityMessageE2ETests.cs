// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class ASCforIoTSecurityMessageE2ETests : IDisposable
    {
        const string SecurityMessageTemplate =
@"{{
        ""AgentVersion"": ""0.0.1.21507"",
        ""AgentId"": ""{0}"",
        ""MessageSchemaVersion"": ""1.0"",
        ""Events"": [{{
            ""EventType"": ""Security"",
            ""Category"": ""Periodic"",
            ""Name"": ""ListeningPorts"",
            ""IsEmpty"": true,
            ""PayloadSchemaVersion"": ""1.0"",
            ""Id"": ""{0}"",
            ""TimestampLocal"": ""{1}"",
            ""TimestampUTC"": ""{1}"",
            ""Payload"": []
          }}]
        }}";

        private readonly string DevicePrefix = $"E2E_{nameof(ASCforIoTSecurityMessageE2ETests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(ASCforIoTSecurityMessageE2ETests)}_";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;
        private readonly ASCforIoTOmsClient _omsClient;

        public ASCforIoTSecurityMessageE2ETests()
        {
            _listener = TestConfig.StartEventListener();
            _omsClient = ASCforIoTOmsClient.CreateClient();
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Amqp()
        {
            await TestSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Amqp()
        {
            await TestSecurityMessageModule(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_AmqpWs()
        {
            await TestSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_AmqpWs()
        {
            await TestSecurityMessageModule(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Mqtt()
        {
            await TestSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_Mqtt()
        {
            await TestSecurityMessageModule(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_MqttWs()
        {
            await TestSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_ModuleSendSingleMessage_MqttWs()
        {
            await TestSecurityMessageModule(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SecurityMessage_DeviceSendSingleMessage_Http()
        {
            await TestSecurityMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        private Client.Message ComposeD2CSecurityTestMessage(out string eventId, out string payload, out string p1Value)
        {
            eventId = p1Value = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;
            payload = string.Format(CultureInfo.InvariantCulture, SecurityMessageTemplate, eventId, now);

            Client.Message message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
            message.SetAsSecurityMessage();

            return message;
        }

        private async Task TestSecurityMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(testDevice.Id).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ASCforIoTOmsClient ascForIotOmsClient = ASCforIoTOmsClient.CreateClient())
            {
                try
                {
                    await SendSingleSecurityMessage(deviceClient, testDevice.Id, testListener, ascForIotOmsClient).ConfigureAwait(false);
                }
                finally
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                    await testListener.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task TestSecurityMessageModule(TestDeviceType type, Client.TransportType transport)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(testModule.Id).ConfigureAwait(false);

            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transport))
            using (ASCforIoTOmsClient ascForIotOmsClient = ASCforIoTOmsClient.CreateClient())
            {
                try
                {
                    await SendSingleSecurityMessageModule(moduleClient, testModule.DeviceId, testListener, ascForIotOmsClient).ConfigureAwait(false);
                }
                finally
                {
                    await moduleClient.CloseAsync().ConfigureAwait(false);
                    await testListener.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task SendSingleSecurityMessage(DeviceClient deviceClient, string deviceId, EventHubTestListener testListener, ASCforIoTOmsClient omsClient)
        {
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Client.Message testMessage = ComposeD2CSecurityTestMessage(out string eventId, out string payload, out string p1Value);
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

            await ValidateEvent(deviceId, eventId, payload, p1Value, testListener, omsClient).ConfigureAwait(false);
        }

        private async Task SendSingleSecurityMessageModule(ModuleClient moduleClient, string deviceId, EventHubTestListener testListener, ASCforIoTOmsClient omsClient)
        {
            await moduleClient.OpenAsync().ConfigureAwait(false);

            Client.Message testMessage = ComposeD2CSecurityTestMessage(out string eventId, out string payload, out string p1Value);
            await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);

            await ValidateEvent(deviceId, eventId, payload, p1Value, testListener, omsClient).ConfigureAwait(false);
        }

        private async Task ValidateEvent(string deviceId, string eventId, string payload, string p1Value,
            EventHubTestListener testListener, ASCforIoTOmsClient omsClient)
        {
            bool isReceivedEventHub = await testListener.WaitForMessage(deviceId, payload, p1Value).ConfigureAwait(false);
            Assert.IsFalse(isReceivedEventHub, "Security message received in customer event hub.");
            bool isReceivedOms = await omsClient.IsRawEventExist(deviceId, eventId).ConfigureAwait(false);
            Assert.IsTrue(isReceivedOms, "Securit message was not recived in customer oms");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _omsClient.Dispose();
            }
        }
    }
}
