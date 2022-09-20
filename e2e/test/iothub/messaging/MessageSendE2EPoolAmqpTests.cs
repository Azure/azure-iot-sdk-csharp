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
    [TestCategory("IoTHub")]
    public class MessageSendE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(MessageSendE2EPoolAmqpTests)}_";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_Amqp()
        {
            await SendMessagePoolOverAmqp(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
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
            int devicesCount)
        {
            async Task InitAsync(DeviceClient deviceClient, TestDevice t, TestDeviceCallbackHandler c)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                using Client.Message testMessage = MessageSendE2ETests.ComposeD2cTestMessage(Logger, out string payload, out string p1Value);
                Logger.Trace($"{nameof(MessageSendE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{testMessage.MessageId}' payload='{payload}' p1Value='{p1Value}'");
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
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
                    ConnectionStringAuthScope.Device,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
