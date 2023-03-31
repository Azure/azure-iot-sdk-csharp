﻿// Copyright (c) Microsoft. All rights reserved.
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

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(5);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponseAsync<T>(string deviceId, DirectMethodServiceRequest directMethodRequest, T expectedClientResponsePayload)
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
                        .InvokeAsync(deviceId, directMethodRequest)
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
                    await Task.Delay(1000).ConfigureAwait(false);
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
            string proxyAddress = null)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;

            // Configure the callback and start accepting method calls.
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload)
                    .ConfigureAwait(false);
            }

            // Call the method from the service side and verify the device received the call.
            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequest, s_deviceResponsePayload);

                using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);

                await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
            }

            // Cleanup references.
            Task CleanupAsync()
            {
                testDeviceCallbackHandler?.Dispose();
                return Task.FromResult(true);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    proxyAddress,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDelay, // we want a quick one because we need time to recover
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupAsync)
                .ConfigureAwait(false);
        }
    }
}
