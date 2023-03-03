// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_DeviceSendSingleMessage_Amqp()
        {
            return TestSecurityMessageAsync(new IotHubClientAmqpSettings());
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_ModuleSendSingleMessage_Amqp()
        {
            return TestSecurityMessageModuleAsync(new IotHubClientAmqpSettings());
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Flaky")]
        public Task SecurityMessage_DeviceSendSingleMessage_AmqpWs()
        {
            return TestSecurityMessageAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_ModuleSendSingleMessage_AmqpWs()
        {
            return TestSecurityMessageModuleAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_DeviceSendSingleMessage_Mqtt()
        {
            return TestSecurityMessageAsync(new IotHubClientMqttSettings());
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_ModuleSendSingleMessage_Mqtt()
        {
            return TestSecurityMessageModuleAsync(new IotHubClientMqttSettings());
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_DeviceSendSingleMessage_MqttWs()
        {
            return TestSecurityMessageAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public Task SecurityMessage_ModuleSendSingleMessage_MqttWs()
        {
            return TestSecurityMessageModuleAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));
        }

        private static TelemetryMessage ComposeD2CSecurityTestMessage()
        {
            string eventId = Guid.NewGuid().ToString();
            string p1Value = eventId;
            string payload = ComposeAzureSecurityCenterForIoTSecurityMessagePayload(eventId);

            var message = new TelemetryMessage(payload)
            {
                Properties = { ["property1"] = p1Value }
            };
            message.SetAsSecurityMessage();

            return message;
        }

        private static string ComposeAzureSecurityCenterForIoTSecurityMessagePayload(string eventId)
        {
            DateTime utcNow = DateTime.UtcNow;

            return JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "AgentVersion", "0.0.1" },
                    { "AgentId" , Guid.NewGuid().ToString() },
                    { "MessageSchemaVersion", "1.0" },
                    {
                        "Events",
                        new[]
                        {
                            new Dictionary<string, object>
                            {
                                { "EventType", "Security" },
                                { "Category", "Periodic" },
                                { "Id", eventId },
                                { "TimestampLocal", utcNow },
                                { "TimestampUTC", utcNow },
                                { "Payload", Array.Empty<object>() },
                            },
                        }
                    },
                });
        }

        private async Task TestSecurityMessageAsync(IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);
            await SendSingleSecurityMessageAsync(deviceClient).ConfigureAwait(false);
        }

        private async Task TestSecurityMessageModuleAsync(IotHubClientTransportSettings transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix).ConfigureAwait(false);

            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, new IotHubClientOptions(transportSettings));
            await moduleClient.OpenAsync().ConfigureAwait(false);
            await SendSingleSecurityMessageModuleAsync(moduleClient).ConfigureAwait(false);
        }

        private static async Task SendSingleSecurityMessageAsync(IotHubDeviceClient deviceClient)
        {
            TelemetryMessage testMessage = ComposeD2CSecurityTestMessage();
            await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
        }

        private static async Task SendSingleSecurityMessageModuleAsync(IotHubModuleClient moduleClient)
        {
            TelemetryMessage testMessage = ComposeD2CSecurityTestMessage();
            await moduleClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
