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
    public class TelemetryE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TelemetryE2EPoolAmqpTests)}_";

        [TestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessagePoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ct).ConfigureAwait(false);
        }

        private async Task SendMessagePoolOverAmqp(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            CancellationToken ct)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _, CancellationToken ct)
            {
                TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(TelemetryE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{testMessage.MessageId}' payload='{payload}' p1Value='{p1Value}'");
                await testDevice.DeviceClient.SendTelemetryAsync(testMessage, ct).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    (d, c, ct) => Task.FromResult(false),
                    TestOperationAsync,
                    null,
                    ConnectionStringAuthScope.Device,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
