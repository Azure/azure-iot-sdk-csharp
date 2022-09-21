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
    public class MessageReceiveE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessageReceiveE2EPoolAmqpTests)}_";

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallback_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallback_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallback_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallback_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallback_PoolOverAmqpAsync(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount)
        {
            // Initialize the service client
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                Logger.Trace($"{nameof(MessageReceiveE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");

                Message msg = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger, out string _, out string _);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

                await serviceClient.Messages.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    ConnectionStringAuthScope.Device,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
