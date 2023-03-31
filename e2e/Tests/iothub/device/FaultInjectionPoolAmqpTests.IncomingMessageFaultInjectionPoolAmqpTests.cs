// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private readonly string MessageReceive_DevicePrefix = $"{nameof(FaultInjectionPoolAmqpTests)}.MessagaeReceive";

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_TcpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_TcpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_AmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_AmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_AmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_AmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IncomingMessage_AmqpC2dLinkDropRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_AmqpC2dLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_GracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_GracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_DeviceSak_GracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task IncomingMessage_DeviceSak_GracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ReceiveIncomingMessageRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var subscribeCallbackCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(subscribeCallbackCts.Token).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                OutgoingMessage msg = OutgoingMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                testDeviceCallbackHandler.ExpectedOutgoingMessage = msg;

                using var sendMessageCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await serviceClient.Messages.SendAsync(testDevice.Id, msg, sendMessageCts.Token).ConfigureAwait(false);

                using var receiveMessageCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(receiveMessageCts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(List<IotHubDeviceClient> deviceClients, List<TestDeviceCallbackHandler> testDeviceCallbackHandlers)
            {
                using var closeCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await serviceClient.Messages.CloseAsync(closeCts.Token).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MessageReceive_DevicePrefix,
                    transportSettings,
                    null,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    FaultInjection.s_defaultFaultDelay,
                    FaultInjection.s_defaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }
    }
}
