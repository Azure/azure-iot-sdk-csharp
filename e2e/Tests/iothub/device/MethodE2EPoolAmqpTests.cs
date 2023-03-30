// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class MethodE2EPoolAmqpTests : E2EMsTestBase
    {
        private const string MethodName = nameof(MethodE2EPoolAmqpTests);
        private readonly string _devicePrefix = $"{MethodName}_";

        private static readonly DirectMethodResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        private static readonly DirectMethodRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondPoolOverAmqp(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Setting method for device {testDevice.Id}");
                await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload, DirectMethodResponsePayload>(s_deviceResponsePayload).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                    testDevice.Id,
                    directMethodRequest,
                    s_deviceResponsePayload);

                await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    null,
                    authScope)
                .ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            T respJson)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName}.");

            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            response.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(respJson));
        }
    }
}
