// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    public class MethodFaultInjectionTests : E2EMsTestBase
    {
        private const string MethodName = "MethodE2ETest";
        private readonly string DevicePrefix = $"{nameof(MethodFaultInjectionTests)}_";

        private static readonly DirectMethodResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        private static readonly DirectMethodRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_GracefulShutdownRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_GracefulShutdownRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_AmqpConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_AmqpSessionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpMethodReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpMethodReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpMethodResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpMethodResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Method_AmqpLinkDropRecovery_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponseAsync<T>(string deviceId, DirectMethodServiceRequest directMethodRequest, T expectedClientResponsePayload, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            bool done = false;
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            int attempt = 0;

            while (!done && sw.Elapsed < FaultInjection.RecoveryTime)
            {
                attempt++;
                try
                {
                    using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

                    VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName} for device {deviceId}.");
                    DirectMethodClientResponse clientResponse = await serviceClient.DirectMethods
                        .InvokeAsync(deviceId, directMethodRequest, ct)
                        .ConfigureAwait(false);

                    VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {clientResponse.Status} for device {deviceId}.");

                    clientResponse.Status.Should().Be(200);
                    clientResponse.TryGetPayload(out T actualClientResponsePayload).Should().BeTrue();
                    JsonConvert.SerializeObject(actualClientResponsePayload).Should().Be(JsonConvert.SerializeObject(expectedClientResponsePayload));

                    done = true;
                }
                catch (IotHubServiceException ex) when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubServiceErrorCode.DeviceNotFound)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                    VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: [Tried {attempt} time(s)] ServiceClient exception caught: {ex}.");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }

            if (!done && exceptionDispatchInfo != null)
            {
                exceptionDispatchInfo.Throw();
            }
        }

        private async Task SendMethodAndRespondRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            // Configure the callback and start accepting method calls.
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await testDevice.DeviceClient.OpenAsync(ct).ConfigureAwait(false);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct)
                    .ConfigureAwait(false);
            }

            // Call the method from the service side and verify the device received the call.
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
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

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    null,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDelay, // we want a quick one because we need time to recover
                    InitOperationAsync,
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
