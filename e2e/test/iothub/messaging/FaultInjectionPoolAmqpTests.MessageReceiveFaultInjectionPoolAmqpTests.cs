// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private readonly string MessageReceive_DevicePrefix = $"{nameof(FaultInjectionPoolAmqpTests)}.MessagaeReceive";

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveUsingCallbackRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveUsingCallbackRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveUsingCallbackRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveUsingCallbackRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveUsingCallbackRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveUsingCallbackRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task Message_AmqpC2dLinkDropReceiveUsingCallbackRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpC2dLinkDropReceiveUsingCallbackRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveUsingCallbackRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveUsingCallbackRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                Message msg = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger, out string payload, out string p1Value);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

                await serviceClient.Messages.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}: Sent message to device {testDevice.Id}: payload='{payload}' p1Value='{p1Value}'");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                Client.Message receivedMessage = await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(List<IotHubDeviceClient> deviceClients, List<TestDeviceCallbackHandler> testDeviceCallbackHandlers)
            {
                await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
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
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    ConnectionStringAuthScope.Device,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
