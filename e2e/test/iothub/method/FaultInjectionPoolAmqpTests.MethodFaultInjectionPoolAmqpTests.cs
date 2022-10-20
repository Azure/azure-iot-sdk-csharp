// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Methods;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private const string MethodDevicePrefix = "MethodFaultInjectionPoolAmqpTests";
        private const string MethodName = "MethodE2EFaultInjectionPoolAmqpTests";

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            string faultType,
            string reason)
        {
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAsync(MethodName, MethodE2ETests.s_deviceResponsePayload, MethodE2ETests.s_serviceRequestPayload)
                    .ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
                Task serviceSendTask = MethodE2ETests
                    .ServiceSendMethodAndVerifyResponseAsync(
                        testDevice.Id,
                        MethodName,
                        MethodE2ETests.s_deviceResponsePayload,
                        MethodE2ETests.s_serviceRequestPayload);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);

                await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MethodDevicePrefix,
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
                    (d, c) => Task.FromResult(false),
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }
    }
}
