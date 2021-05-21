// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Telemetry
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public partial class TelemetrySendE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(TelemetrySendE2ETests)}_";
        private readonly string ModulePrefix = $"{nameof(TelemetrySendE2ETests)}_";

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetry_Mqtt()
        {
            await SendTelemetryAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetry_MqttWs()
        {
            await SendTelemetryAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetryWithComponent_Mqtt()
        {
            await SendTelemetryWithComponentAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetryWithComponent_MqttWs()
        {
            await SendTelemetryWithComponentAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        private async Task SendTelemetryAsync(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, testDevice.Id, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendTelemetryWithComponentAsync(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageWithComponentAsync(deviceClient, testDevice.Id, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task SendSingleMessageAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger)
        {
            Client.TelemetryMessage testMessage;
            (testMessage, _) = ComposeTelemetryMessage(logger);

            using (testMessage)
            {
                await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
            }
        }

        public static async Task SendSingleMessageWithComponentAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger)
        {
            Client.TelemetryMessage testMessage;
            (testMessage, _) = ComposeTelemetryMessageWithComponent(logger);

            using (testMessage)
            {
                await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
            }
        }

        public static (Client.TelemetryMessage message, string p1Value) ComposeTelemetryMessageWithComponent(MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();
            string componentName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeTelemetryMessageWithComponent)}: messageId='{messageId}' userId='{userId}' p1Value='{p1Value}'");
            var message = new TelemetryMessage
            {
                MessageId = messageId,
                UserId = userId,
                ComponentName = componentName,
                Telemetry = {
                   { "property1", p1Value },
                   { "property2", null},
                }
            };

            return (message, p1Value);
        }

        public static (Client.TelemetryMessage message, string p1Value) ComposeTelemetryMessage(MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeTelemetryMessage)}: messageId='{messageId}' userId='{userId}' p1Value='{p1Value}'");
            var message = new TelemetryMessage
            {
                MessageId = messageId,
                UserId = userId,
                Telemetry = {
                   { "property1", p1Value },
                   { "property2", null},
                }
            };

            return (message, p1Value);
        }

    }
}
