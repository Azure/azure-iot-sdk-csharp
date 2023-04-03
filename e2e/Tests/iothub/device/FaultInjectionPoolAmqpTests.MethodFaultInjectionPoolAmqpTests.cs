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

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpConn, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpConn, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Method_ConnectionLossRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_AmqpSessionLossRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpMethodReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpMethodReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpMethodResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpMethodResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Method_AmqpLinkDropRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            async Task InitOperationAsync(TestDevice _, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct)
                    .ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {

                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");

                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequest, s_deviceResponsePayload, ct);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);

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
                    (d, c, ct) => Task.FromResult(false),
                    ct)
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
