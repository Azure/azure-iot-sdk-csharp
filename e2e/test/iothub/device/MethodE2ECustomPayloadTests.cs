// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
    [TestCategory("IoTHub")]
    public class MethodE2ECustomPayloadTests : E2EMsTestBase
    {
        private static readonly CustomType _customTypeRequest = new("request", 21, false, new("e2e_test_request", 12));
        private static readonly CustomType _customTypeResponse = new("response", 21, false, new("e2e_test_response", 12));
        private static readonly bool _booleanRequest = true;
        private static readonly byte[] _arrayRequest = new byte[] { 1, 2, 3 };
        private static readonly byte[] _arrayResponse = new byte[] { 3, 2, 1 };
        private static readonly List<double> _listRequest = new() { 1.0, 2.0, 3.0 };
        private static readonly List<double> _listResponse = new() { 3.0, 2.0, 1.0 };
        private static readonly Dictionary<string, object> _dictRequest = new() { { "key1", 2.0 }, { "key2", "val" } };
        private static readonly Dictionary<string, object> _dictResponse = new() { { "key1", new byte[] { 3, 5, 6 } }, { "key2", false } };

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private readonly string _modulePrefix = $"{nameof(MethodE2ETests)}_mod_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodTimeoutMinutes = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomPayload_Mqtt()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_customPayloadAsync, _customTypeRequest, _customTypeResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_BooleanPayload_Mqtt()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_booleanPayloadAsync, _booleanRequest, false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_ArrayPayload_Mqtt()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_ArrayPayloadAsync, _arrayRequest, _arrayResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_ListPayload_Mqtt()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_listPayloadAsync, _listRequest, _listResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_DictionaryPayload_Mqtt()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_dictionaryPayloadAsync, _dictRequest, _dictResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_CustomPayload_Amqp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethod_customPayloadAsync, _customTypeRequest, _customTypeResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_BooleanPayload_Amqp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethod_booleanPayloadAsync, _booleanRequest, false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_ArrayPayload_Amqp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethod_ArrayPayloadAsync, _arrayRequest, _arrayResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_ListPayload_Amqp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethod_listPayloadAsync, _listRequest, _listResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_DictionaryPayload_Amqp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethod_dictionaryPayloadAsync, _dictRequest, _dictResponse).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            object request,
            object response,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await TestDevice.OpenWithRetryAsync(deviceClient).ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                response,
                request,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            string methodName,
            object response,
            T request,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            var serviceClient = TestDevice.ServiceClient;
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ResponseTimeout = methodTimeout,
                Payload = request,
            };

            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {methodResponse.Status}.");
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().BeEquivalentTo(JsonConvert.SerializeObject(response));
        }

        public static async Task<Task> SetDeviceReceiveMethod_booleanPayloadAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethod_booleanPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.GetPayloadAsJsonString().Should().Be(JsonConvert.SerializeObject(_booleanRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_booleanRequest)} but was {request.GetPayloadAsJsonString()}");
                            _booleanRequest.Should().BeTrue();

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = false,
                        };

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_customPayloadAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethod_customPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.GetPayloadAsJsonString().Should().Be(JsonConvert.SerializeObject(_customTypeRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_customTypeRequest)} but was {request.GetPayloadAsJsonString()}");
                            request.TryGetPayload(out CustomType customType).Should().BeTrue();
                            customType.Should().BeEquivalentTo(_customTypeRequest, $"The expected respose payload should be {_customTypeRequest} but was {customType}");

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = _customTypeResponse
                        };

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_listPayloadAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethod_listPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.GetPayloadAsJsonString().Should().Be(JsonConvert.SerializeObject(_listRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_listRequest)} but was {request.GetPayloadAsJsonString()}");
                            request.TryGetPayload(out List<double> listRequest).Should().BeTrue();
                            listRequest.Should().BeEquivalentTo(_listRequest, $"The expected respose payload should be {_listRequest} but was {listRequest}");

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = _listResponse,
                        };

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_dictionaryPayloadAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethod_dictionaryPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.GetPayloadAsJsonString().Should().Be(JsonConvert.SerializeObject(_dictRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_dictRequest)} but was {request.GetPayloadAsJsonString()}");
                            request.TryGetPayload(out Dictionary<string, object> dictRequest).Should().BeTrue();
                            dictRequest.Should().BeEquivalentTo(_dictRequest, $"The expected respose payload should be {_dictRequest} but was {dictRequest}");

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }
                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = _dictResponse,
                        };

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_ArrayPayloadAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethod_ArrayPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.GetPayloadAsJsonString().Should().Be(JsonConvert.SerializeObject(_arrayRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_arrayRequest)} but was {request.GetPayloadAsJsonString()}");
                            request.TryGetPayload(out byte[] byteRequest).Should().BeTrue();
                            byteRequest.Should().BeEquivalentTo(_arrayRequest, $"The expected respose payload should be {_arrayRequest} but was {byteRequest}");

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }
                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = _arrayResponse,
                        };

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }
    }
}
