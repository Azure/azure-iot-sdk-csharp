// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class DirectMethodE2eCustomPayloadTests : E2EMsTestBase
    {
        private static readonly DirectMethodRequestPayload _customTypeRequest = new() { DesiredState = "on" };
        private static readonly DirectMethodResponsePayload _customTypeResponse = new() { CurrentState = "off" };
        private static readonly byte[] _arrayRequest = new byte[] { 1, 2, 3 };
        private static readonly byte[] _arrayResponse = new byte[] { 3, 2, 1 };
        private static readonly List<double> _listRequest = new() { 1.0, 2.0, 3.0 };
        private static readonly List<double> _listResponse = new() { 3.0, 2.0, 1.0 };
        private static readonly Dictionary<string, object> _dictRequest = new() { { "key1", 2.0 }, { "key2", "val" } };
        private static readonly Dictionary<string, object> _dictResponse = new() { { "key1", new byte[] { 3, 5, 6 } }, { "key2", false } };

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(1);

        // The deserialization code is the same irrespective of if the client was initialized over TCP or WS.
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomType_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _customTypeRequest, _customTypeResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Boolean_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), true, false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Array_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _arrayRequest, _arrayResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_List_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _listRequest, _listResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Dictionary_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), _dictRequest, _dictResponse).ConfigureAwait(false);
        }

        // The deserialization code is the same irrespective of if the client was initialized over TCP or WS.
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomType_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _customTypeRequest, _customTypeResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Boolean_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), true, false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Array_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _arrayRequest, _arrayResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_List_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _listRequest, _listResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Dictionary_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), _dictRequest, _dictResponse).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync<T,H>(
            IotHubClientTransportSettings transportSettings,
            T directMethodRequestFromService,
            H directMethodResponseFromClient)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<T>(directMethodResponseFromClient);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
                Payload = directMethodRequestFromService,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, directMethodRequest, directMethodResponseFromClient);

            using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            DirectMethodServiceRequest directMethodRequest,
            T expectedClientResponsePayload)
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName} for device {deviceId}.");

            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method response status: {methodResponse.Status} for device {deviceId}.");
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out T actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(expectedClientResponsePayload));
        }
    }
}
