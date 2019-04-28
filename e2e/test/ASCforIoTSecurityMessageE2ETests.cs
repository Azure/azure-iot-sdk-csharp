// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class ASCforIoTSecurityMessageE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(ASCforIoTSecurityMessageE2ETests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(ASCforIoTSecurityMessageE2ETests)}_";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;
        private readonly ASCforIoTLogAnalyticsClient _logAnalyticsClient;

        public ASCforIoTSecurityMessageE2ETests()
        {
            _listener = TestConfig.StartEventListener();
            _logAnalyticsClient = ASCforIoTLogAnalyticsClient.CreateClient();
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
            payload = ComposeASCforIoTSecurityMessagePayload(eventId).ToString(Newtonsoft.Json.Formatting.None);

            Client.Message message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
            message.SetAsSecurityMessage();

            return message;
        }

        private JObject ComposeASCforIoTSecurityMessagePayload(string eventId)
        {
            var now = DateTime.UtcNow;
            return new JObject
            {
                { "AgentVersion", "0.0.1" },
                { "AgentId" , Guid.NewGuid().ToString() },
                { "MessageSchemaVersion", "1.0" },
                { "Events", new JArray
                    { new JObject
                        {
                            { "EventType", "Security" },
                            { "Category", "Periodic" },
                            { "Name", "ListeningPorts" },
                            { "IsEmpty", true },
                            { "PayloadSchemaVersion", "1.0" },
                            { "Id", eventId },
                            { "TimestampLocal", now },
                            { "TimestampUTC", now },
                            { "Payload", new JArray() }
                        }
                    }
                }
            };
        }

        private async Task TestSecurityMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            EventHubTestListener testListener = await EventHubTestListener.CreateListener(testDevice.Id).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                try
                {
                    await SendSingleSecurityMessage(deviceClient, testDevice.Id, testListener, _logAnalyticsClient).ConfigureAwait(false);
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
            {
                try
                {
                    await SendSingleSecurityMessageModule(moduleClient, testModule.DeviceId, testListener, _logAnalyticsClient).ConfigureAwait(false);
                }
                finally
                {
                    await moduleClient.CloseAsync().ConfigureAwait(false);
                    await testListener.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task SendSingleSecurityMessage(DeviceClient deviceClient, string deviceId, EventHubTestListener testListener, ASCforIoTLogAnalyticsClient logAnalticsTestClient)
        {
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Client.Message testMessage = ComposeD2CSecurityTestMessage(out string eventId, out string payload, out string p1Value);
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

            await ValidateEvent(deviceId, eventId, payload, p1Value, testListener, logAnalticsTestClient).ConfigureAwait(false);
        }

        private async Task SendSingleSecurityMessageModule(ModuleClient moduleClient, string deviceId, EventHubTestListener testListener, ASCforIoTLogAnalyticsClient logAnalticsTestClient)
        {
            await moduleClient.OpenAsync().ConfigureAwait(false);

            Client.Message testMessage = ComposeD2CSecurityTestMessage(out string eventId, out string payload, out string p1Value);
            await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);

            await ValidateEvent(deviceId, eventId, payload, p1Value, testListener, logAnalticsTestClient).ConfigureAwait(false);
        }

        private async Task ValidateEvent(string deviceId, string eventId, string payload, string p1Value,
            EventHubTestListener testListener, ASCforIoTLogAnalyticsClient logAnalticsTestClient)
        {
            bool isReceivedEventHub = await testListener.WaitForMessage(deviceId, payload, p1Value).ConfigureAwait(false);
            Assert.IsFalse(isReceivedEventHub, "Security message received in customer event hub.");
            bool isReceivedOms = await logAnalticsTestClient.IsRawEventExist(deviceId, eventId).ConfigureAwait(false);
            Assert.IsTrue(isReceivedOms, "Security message was not recived in customer log analytics");
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
                _logAnalyticsClient.Dispose();
            }
        }
    }
}
