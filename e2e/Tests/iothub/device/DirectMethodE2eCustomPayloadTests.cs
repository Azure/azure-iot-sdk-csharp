// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class DirectMethodE2eCustomPayloadTests : E2EMsTestBase
    {
        private static readonly DirectMethodRequestPayload _customTypeRequest = new() { DesiredState = "on" };
        private static readonly DirectMethodResponsePayload _customTypeResponse = new() { CurrentState = "off" };
        private static readonly JsonElement _listRequest = JsonSerializer.SerializeToElement(new List<double>() { 1.0, 2.0, 3.0 });
        private static readonly JsonElement _listResponse = JsonSerializer.SerializeToElement(new List<double>() { 3.0, 2.0, 1.0 });
        //private static readonly JsonElement _dictRequest = JsonSerializer({ { "key1", 2.0 }, { "key2", "val" } });
        //private static readonly JsonElement _dictResponse = JsonSerializer.SerializeToElement(new Dictionary<string, object> { { "key1", new byte[] { 3, 5, 6 } }, { "key2", false } });

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private const string MethodName = "MethodE2ETest";

        private static readonly int s_defaultMethodResponseTimeout = 30;

        // The deserialization code is the same irrespective of if the client was initialized over TCP or WS.
        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomType_MqttWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _customTypeRequest, _customTypeResponse, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Boolean_MqttWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), true, false, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_List_MqttWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _listRequest, _listResponse, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Dictionary_MqttWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _dictRequest, _dictResponse, ct).ConfigureAwait(false);
        }

        // The deserialization code is the same irrespective of if the client was initialized over TCP or WS.
        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomType_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _customTypeRequest, _customTypeResponse, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Boolean_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), true, false, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_List_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _listRequest, _listResponse, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Dictionary_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _dictRequest, _dictResponse, ct).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            T directMethodRequestFromService,
            H directMethodResponseFromClient,
            CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<T>(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(directMethodResponseFromClient)), ct);

            DirectMethodServiceRequest directMethodRequest;
            if (typeof(T) == typeof(byte[]))
            {
                directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
                };
                directMethodRequest.SetPayload(directMethodRequestFromService);
            }
            else
            {
                directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout,
                };

                directMethodRequest.SetPayload(directMethodRequestFromService);
            }

            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequest, directMethodResponseFromClient, ct);
            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            JsonElement expectedClientResponsePayload,
            CancellationToken ct)
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName} for device {deviceId}.");

            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest, ct)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method response status: {methodResponse.Status} for device {deviceId}.");
            methodResponse.Status.Should().Be(200);
            JsonSerializer.Serialize(expectedClientResponsePayload).Should().Equals(JsonSerializer.Serialize(methodResponse.JsonPayload)); // Sensitive to order within the json object, but good enough for this test
        }
    }
}
