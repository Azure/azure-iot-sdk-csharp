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
using Microsoft.Azure.Devices.E2ETests.helpers;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class DirectMethodE2eCustomPayloadTests : E2EMsTestBase
    {

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private const string MethodName = "MethodE2ETest";

        private static readonly int s_defaultMethodResponseTimeout = 30;

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomStronglyTypedObject(Protocol protocol)
        {
            DirectMethodRequestPayload _customTypeRequest = new() { DesiredState = "on" };
            DirectMethodResponsePayload _customTypeResponse = new() { CurrentState = "off" };

        DirectMethodServiceRequest request = new(MethodName)
            { 
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload(_customTypeRequest);

            DirectMethodResponse response = new(200);
            response.SetPayload(_customTypeResponse);
            
            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_IntPayload(Protocol protocol)
        {
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload(1);

            DirectMethodResponse response = new(200);
            response.SetPayload(2);

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_StringPayload(Protocol protocol)
        {
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload("someString");

            DirectMethodResponse response = new(200);
            response.SetPayload("someOtherString");

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_BoolPayload(Protocol protocol)
        {
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload(true);

            DirectMethodResponse response = new(200);
            response.SetPayload(false);

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_ArrayPayload(Protocol protocol)
        {
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload(new List<int>() { 1, 2, 3 });

            DirectMethodResponse response = new(200);
            response.SetPayload(new List<int>() { 3, 2, 1 });

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }


        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_ComplexPayload(Protocol protocol)
        {
            // Combine all the above cases into one complex payload type
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayload(new List<object>() { 1, false, "someString", new List<object>() { "nestedString", true, new DirectMethodRequestPayload() { DesiredState = "on"} } });

            DirectMethodResponse response = new(200);
            response.SetPayload(new List<object>() { 1, true, "someOtherString", new List<object>() { "anotherNestedString", true, new DirectMethodRequestPayload() { DesiredState = "off" } } });

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Protocol.Amqp)]
        [DataRow(Protocol.Mqtt)]
        public async Task Method_DeviceReceivesMethodAndResponse_RawJsonStrings(Protocol protocol)
        {
            // Combine all the above cases into one complex payload type
            DirectMethodServiceRequest request = new(MethodName)
            {
                ResponseTimeoutInSeconds = s_defaultMethodResponseTimeout
            };
            request.SetPayloadJson("{\"someKey1\":\"someValue1\"}");

            DirectMethodResponse response = new(200);
            response.SetPayloadJson("{\"someKey2\":\"someValue2\"}");

            await SendMethodAndRespondAsync(protocol, request, response).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            Protocol protocol,
            DirectMethodServiceRequest directMethodRequestFromService,
            DirectMethodResponse directMethodResponseFromClient)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);

            IotHubClientTransportSettings transportSettings;
            if (protocol == Protocol.Amqp)
            {
                transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            }
            else
            {
                transportSettings = new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket);
            }

            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync(directMethodResponseFromClient.Payload, ct);

            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequestFromService;

            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequestFromService, directMethodResponseFromClient.Payload, ct);
            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            byte[] expectedClientResponsePayload,
            CancellationToken ct)
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName} for device {deviceId}.");

            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest, ct)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method response status: {methodResponse.Status} for device {deviceId}.");
            methodResponse.Status.Should().Be(200);
            expectedClientResponsePayload.Should().Equal(JsonSerializer.SerializeToUtf8Bytes(methodResponse.JsonPayload)); // Sensitive to order within the json object, but good enough for this test
        }
    }
}
