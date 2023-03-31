// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
<<<<<<< HEAD:e2e/Tests/iothub/device/TelemetryMessageSendE2ePoolAmqpTests.cs
    public class TelemetryMessageSendE2ePoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TelemetryMessageSendE2ePoolAmqpTests)}_";
=======
    public class TelemetryE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TelemetryE2EPoolAmqpTests)}_";
>>>>>>> ff5df70b6 (wip):e2e/Tests/iothub/device/TelemetryE2EPoolAmqpTests.cs

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        private async Task SendMessagePoolOverAmqp(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            async Task InitAsync(TestDevice testDevice, TestDeviceCallbackHandler c)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await testDevice.OpenWithRetryAsync(cts.Token).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _)
            {
<<<<<<< HEAD:e2e/Tests/iothub/device/TelemetryMessageSendE2ePoolAmqpTests.cs
                TelemetryMessage testMessage = TelemetryMessageE2eTests.ComposeD2cTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(TelemetryMessageSendE2ePoolAmqpTests)}.{testDevice.Id}: messageId='{testMessage.MessageId}' payload='{payload}' p1Value='{p1Value}'");
=======
                TelemetryMessage testMessage = TelemetryE2ETests.ComposeD2cTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(TelemetryE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{testMessage.MessageId}' payload='{payload}' p1Value='{p1Value}'");
>>>>>>> ff5df70b6 (wip):e2e/Tests/iothub/device/TelemetryE2EPoolAmqpTests.cs
                await testDevice.DeviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitAsync,
                    TestOperationAsync,
                    null,
                    authScope)
                .ConfigureAwait(false);
        }
    }
}
