// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("LongRunning")]
    public class AzureSecurityCenterForIoTSecurityMessageE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(AzureSecurityCenterForIoTSecurityMessageE2ETests)}_";
        private readonly string _modulePrefix = $"{nameof(AzureSecurityCenterForIoTSecurityMessageE2ETests)}_";

        [LoggedTestMethod]
        public Task SecurityMessage_DeviceSendSingleMessage_Amqp()
        {
            return TestSecurityMessageAsync(new AmqpTransportSettings());
        }

        [LoggedTestMethod]
        public Task SecurityMessage_ModuleSendSingleMessage_Amqp()
        {
            return TestSecurityMessageModuleAsync(new AmqpTransportSettings());
        }

        [LoggedTestMethod]
        [TestCategory("Flaky")]
        public Task SecurityMessage_DeviceSendSingleMessage_AmqpWs()
        {
            return TestSecurityMessageAsync(new AmqpTransportSettings());
        }

        [LoggedTestMethod]
        public Task SecurityMessage_ModuleSendSingleMessage_AmqpWs()
        {
            return TestSecurityMessageModuleAsync(new AmqpTransportSettings());
        }

        [LoggedTestMethod]
        public Task SecurityMessage_DeviceSendSingleMessage_Mqtt()
        {
            return TestSecurityMessageAsync(new MqttTransportSettings());
        }

        [LoggedTestMethod]
        public Task SecurityMessage_ModuleSendSingleMessage_Mqtt()
        {
            return TestSecurityMessageModuleAsync(new MqttTransportSettings());
        }

        [LoggedTestMethod]
        public Task SecurityMessage_DeviceSendSingleMessage_MqttWs()
        {
            return TestSecurityMessageAsync(new MqttTransportSettings(TransportProtocol.WebSocket));
        }

        [LoggedTestMethod]
        public Task SecurityMessage_ModuleSendSingleMessage_MqttWs()
        {
            return TestSecurityMessageModuleAsync(new MqttTransportSettings(TransportProtocol.WebSocket));
        }

        [LoggedTestMethod]
        public Task SecurityMessage_DeviceSendSingleMessage_Http()
        {
            return TestSecurityMessageAsync(new Client.HttpTransportSettings());
        }

        private Client.Message ComposeD2CSecurityTestMessage()
        {
            string eventId = Guid.NewGuid().ToString();
            string p1Value = eventId;
            string payload = AzureSecurityCenterForIoTSecurityMessageE2ETests.ComposeAzureSecurityCenterForIoTSecurityMessagePayload(eventId).ToString(Newtonsoft.Json.Formatting.None);

            var message = new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
            message.SetAsSecurityMessage();

            return message;
        }

        private static JObject ComposeAzureSecurityCenterForIoTSecurityMessagePayload(string eventId)
        {
            DateTime now = DateTime.UtcNow;
            return new JObject
            {
                { "AgentVersion", "0.0.1" },
                { "AgentId" , Guid.NewGuid().ToString() },
                { "MessageSchemaVersion", "1.0" },
                { "Events", new JArray
                    {
                        new JObject
                        {
                            { "EventType", "Security" },
                            { "Category", "Periodic" },
                            { "Name", "ListeningPorts" },
                            { "IsEmpty", true },
                            { "PayloadSchemaVersion", "1.0" },
                            { "Id", eventId },
                            { "TimestampLocal", now },
                            { "TimestampUTC", now },
                            { "Payload", new JArray() },
                        }
                    }
                }
            };
        }

        private async Task TestSecurityMessageAsync(ITransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ClientOptions(transportSettings));

            try
            {
                await SendSingleSecurityMessageAsync(deviceClient).ConfigureAwait(false);
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task TestSecurityMessageModuleAsync(ITransportSettings transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);

            var options = new ClientOptions(transportSettings);
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, options);
            try
            {
                await SendSingleSecurityMessageModuleAsync(moduleClient).ConfigureAwait(false);
            }
            finally
            {
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendSingleSecurityMessageAsync(
            DeviceClient deviceClient)
        {
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using Client.Message testMessage = ComposeD2CSecurityTestMessage();
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
        }

        private async Task SendSingleSecurityMessageModuleAsync(
            ModuleClient moduleClient)
        {
            await moduleClient.OpenAsync().ConfigureAwait(false);
            using Client.Message testMessage = ComposeD2CSecurityTestMessage();
            await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
