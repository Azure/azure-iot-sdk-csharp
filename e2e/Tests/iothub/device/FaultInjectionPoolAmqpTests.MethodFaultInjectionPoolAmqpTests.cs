// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Methods;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private const string MethodDevicePrefix = "MethodFaultInjectionPoolAmqpTests";
        private const string MethodName = "MethodE2EFaultInjectionPoolAmqpTests";

        private static readonly DirectMethodResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        private static readonly DirectMethodRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount    ,
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
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason)
        {
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var subscribeCallbackCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, subscribeCallbackCts.Token)
                    .ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {

                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");

                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                using var invokeMethodCts = new CancellationTokenSource(s_defaultOperationTimeout);
                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequest, s_deviceResponsePayload, invokeMethodCts.Token);

                using var receiveMethodCts = new CancellationTokenSource(s_defaultOperationTimeout);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(receiveMethodCts.Token);

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
                    FaultInjection.s_defaultFaultDelay,
                    FaultInjection.s_defaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    (d, c) => Task.FromResult(false),
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            T expectedClientResponsePayload,
            CancellationToken ct)
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName} for device {deviceId}.");

            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest, ct)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method response status: {methodResponse.Status} for device {deviceId}.");
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out T actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(expectedClientResponsePayload));
        }
    }
}
