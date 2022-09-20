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
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackRecoveryPoolOverAmqpAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason)
        {
            // Initialize the service client
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);

                msg.Dispose();
            }

            async Task CleanupOperationAsync(List<IotHubDeviceClient> deviceClients, List<TestDeviceCallbackHandler> testDeviceCallbackHandlers)
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MessageReceive_DevicePrefix,
                    transport,
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
