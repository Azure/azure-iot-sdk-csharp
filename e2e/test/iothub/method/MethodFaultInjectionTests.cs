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

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class MethodFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MethodFaultInjectionTests)}_";
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private const string MethodName = "MethodE2ETest";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpMethodReq,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpMethodResp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponseAsync(string deviceName, string methodName, string respJson, string reqJson)
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
                    using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

                    var directMethodRequest = new DirectMethodRequest()
                    {
                        MethodName = methodName,
                        Payload = reqJson,
                        ResponseTimeout = TimeSpan.FromMinutes(5),
                    };

                    Logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
                    DirectMethodResponse response = await serviceClient.DirectMethods
                            .InvokeAsync(deviceName, directMethodRequest)
                            .ConfigureAwait(false);

                    Logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");

                    response.Status.Should().Be(200);
                    ((string)response.Payload).Should().Be(respJson);

                    done = true;
                }
                catch (IotHubServiceException ex) when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubErrorCode.DeviceNotFound)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                    Logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: [Tried {attempt} time(s)] ServiceClient exception caught: {ex}.");
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
            async Task InitAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAsync(MethodName, DeviceResponseJson, ServiceRequestJson)
                    .ConfigureAwait(false);
            }

            // Call the method from the service side and verify the device received the call.
            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson);

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
                    FaultInjection.DefaultFaultDelay,
                    InitAsync,
                    TestOperationAsync,
                    CleanupAsync,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
