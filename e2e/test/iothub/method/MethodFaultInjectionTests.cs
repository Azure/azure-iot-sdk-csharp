// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
                new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientMqttSettings(),
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientMqttSettings(),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecoveryAsync(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponseAsync(string deviceName, string methodName, string respJson, string reqJson)
        {
            var sw = new Stopwatch();
            sw.Start();
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
                            .InvokeAsync(deviceName, directMethodRequest).ConfigureAwait(false);

                    Logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");

                    Assert.AreEqual(200, response.Status, $"The expected respose status should be 200 but was {response.Status}");
                    string payload = JsonConvert.SerializeObject(response.Payload);
                    Assert.AreEqual(respJson, payload, $"The expected respose payload should be {respJson} but was {payload}");

                    done = true;
                }
                catch (IotHubServiceException ex)
                    when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubErrorCode.DeviceNotFound)
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

        private async Task SendMethodAndRespondRecoveryAsync(IotHubClientTransportSettings transportSettings, string faultType, string reason, TimeSpan delayInSec, string proxyAddress = null)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

            // Configure the callback and start accepting method calls.
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAsync(MethodName, DeviceResponseJson, ServiceRequestJson)
                    .ConfigureAwait(false);
            }

            // Call the method from the service side and verify the device received the call.
            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);

                var tasks = new List<Task>() { serviceSendTask, methodReceivedTask };
                while (tasks.Count > 0)
                {
                    Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                    completedTask.GetAwaiter().GetResult();
                    tasks.Remove(completedTask);
                }
            }

            // Cleanup references.
            Task CleanupOperationAsync()
            {
                testDeviceCallbackHandler?.Dispose();
                return Task.FromResult(false);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    proxyAddress,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultFaultDelay,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
