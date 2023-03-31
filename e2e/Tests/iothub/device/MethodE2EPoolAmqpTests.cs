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

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.IotHub)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.IotHub)]
        public async Task Method_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, ConnectionStringAuthScope authScope)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondPoolOverAmqp(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondPoolOverAmqp(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            CancellationToken ct)
        {
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Setting method for device {testDevice.Id}");
                await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct).ConfigureAwait(false);
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

                using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
                Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                    testDevice.Id,
                    directMethodRequest,
                    s_deviceResponsePayload,
                    ct);

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
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            T respJson,
            CancellationToken ct)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName}.");

            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest, ct)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            response.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(respJson));
        }
    }
}
