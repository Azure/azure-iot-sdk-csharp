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
        private const int MessageBatchCount = 5;
        private const int LargeMessageSizeInBytes = 255 * 1024; // The maximum message size for device to cloud messages is 256 KB. We are allowing 1 KB of buffer for message header information etc.
        private readonly string DevicePrefix = $"{nameof(TelemetrySendE2ETests)}_";
        private readonly string ModulePrefix = $"{nameof(TelemetrySendE2ETests)}_";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetry_Mqtt()
        {
            await SendTelemetry(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetry_MqttWs()
        {
            await SendTelemetry(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_DeviceSendSingleTelemetry_Mqtt()
        {
            await SendTelemetry(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleTelemetry_MqttWs()
        {
            await SendTelemetry(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        public async Task Telemetry_DeviceSendSingleLargeTelemetryAsync(TestDeviceType testDeviceType, Client.TransportType transportType, int messageSize)
        {
            await SendTelemetry(testDeviceType, transportType, messageSize).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetryWithComponent_Mqtt()
        {
            await SendTelemetryWithComponent(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Telemetry_DeviceSendSingleTelemetryWithComponent_MqttWs()
        {
            await SendTelemetryWithComponent(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_DeviceSendSingleTelemetryWithComponent_Mqtt()
        {
            await SendTelemetryWithComponent(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceSendSingleTelemetryWithComponent_MqttWs()
        {
            await SendTelemetryWithComponent(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only, LargeMessageSizeInBytes)]
        [DataRow(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only, LargeMessageSizeInBytes)]
        public async Task Telemetry_DeviceSendSingleLargeTelemetryWithComponentAsync(TestDeviceType testDeviceType, Client.TransportType transportType, int messageSize)
        {
            await SendTelemetryWithComponent(testDeviceType, transportType, messageSize).ConfigureAwait(false);
        }

        private async Task SendTelemetry(TestDeviceType type, Client.TransportType transport, int messageSize = 0)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageAsync(deviceClient, testDevice.Id, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendTelemetryWithComponent(TestDeviceType type, Client.TransportType transport, int messageSize = 0)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageWithComponentAsync(deviceClient, testDevice.Id, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendTelemetryWithComponent(TestDeviceType type, ITransportSettings[] transportSettings, int messageSize = 0)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await SendSingleMessageWithComponentAsync(deviceClient, testDevice.Id, Logger, messageSize).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task SendSingleMessageAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger, int messageSize = 0)
        {
            Client.TelemetryMessage testMessage;

            if (messageSize == 0)
            {
                (testMessage, _) = ComposeTelemetryMessage(logger);
            }
            else
            {
                (testMessage, _) = ComposeComplexTelemetryMessage(messageSize, logger);
            }

            using (testMessage)
            {
                await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
            }
        }

        public static async Task SendSingleMessageWithComponentAsync(DeviceClient deviceClient, string deviceId, MsTestLogger logger, int messageSize = 0)
        {
            Client.TelemetryMessage testMessage;

            if (messageSize == 0)
            {
                (testMessage, _) = ComposeTelemetryMessageWithComponent(logger);
            }
            else
            {
                (testMessage, _) = ComposeComplexTelemetryMessageWithComponent(messageSize, logger);
            }

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

            logger.Trace($"{nameof(ComposeTelemetryMessage)}: messageId='{messageId}' userId='{userId}' p1Value='{p1Value}'");
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

        public static (Client.TelemetryMessage message, string p1Value) ComposeComplexTelemetryMessage(int messageSize, MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeComplexTelemetryMessage)}: messageId='{messageId}' p1Value='{p1Value}'");
            var message = new TelemetryMessage
            {
                MessageId = messageId,
            };
            message.Telemetry.Add("property1", p1Value);
            message.Telemetry.Add("property2", null);
            message.Telemetry.Add("property3", new
            {
                complexName = "complexName",
                complexDouble = 2.0,
                complexLargeString = "1".PadLeft(messageSize),
                complexObject = new
                {
                    nestedObjectString = "string",
                    nestedObjectFloat = 1.0325f
                }
            }); ;

            return (message, p1Value);
        }

        public static (Client.TelemetryMessage message, string p1Value) ComposeTelemetryMessage(MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();
            string componentName = Guid.NewGuid().ToString();

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

        public static (Client.TelemetryMessage message, string p1Value) ComposeComplexTelemetryMessageWithComponent(int messageSize, MsTestLogger logger)
        {
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string componentName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeComplexTelemetryMessage)}: messageId='{messageId}' p1Value='{p1Value}'");
            var message = new TelemetryMessage
            {
                MessageId = messageId,
                ComponentName = componentName,
            };
            message.Telemetry.Add("property1", p1Value);
            message.Telemetry.Add("property2", null);
            message.Telemetry.Add("property3", new
            {
                complexName = "complexName",
                complexDouble = 2.0,
                complexLargeString = "1".PadLeft(messageSize),
                complexObject = new
                {
                    nestedObjectString = "string",
                    nestedObjectFloat = 1.0325f
                }
            }); ;

            return (message, p1Value);
        }
    }
}
