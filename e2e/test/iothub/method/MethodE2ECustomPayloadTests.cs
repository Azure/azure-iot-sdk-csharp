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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_customPayload()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_customPayloadAsync, _customTypeRequest, _customTypeResponse).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_booleanPayload()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_booleanPayloadAsync, _booleanRequest, false).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_arrayPayload()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_arrayPayloadAsync, _arrayRequest, _arrayResponse).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_ListPayload()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_listPayloadAsync, _listRequest, _listResponse).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_DictionaryPayload()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethod_dictionaryPayloadAsync, _dictRequest, _dictResponse).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            object request,
            object response,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                response,
                request,
                Logger,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string methodName,
            object response,
            object request,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                ResponseTimeout = methodTimeout,
                Payload = request,
            };

            DirectMethodResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {methodResponse.Status}.");
            methodResponse.Status.Should().Be(200);
            JsonConvert.SerializeObject(methodResponse.Payload).Should().BeEquivalentTo(JsonConvert.SerializeObject(response));
        }

        public static async Task<Task> SetDeviceReceiveMethod_booleanPayloadAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        logger.Trace($"{nameof(SetDeviceReceiveMethod_booleanPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.PayloadAsJsonString.Should().Be(JsonConvert.SerializeObject(_booleanRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_booleanRequest)} but was {request.PayloadAsJsonString}");
                            _booleanRequest.Should().BeTrue();

                            methodCallReceived.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = false,
                        };

                        return Task.FromResult(response);
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_customPayloadAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        logger.Trace($"{nameof(SetDeviceReceiveMethod_customPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.PayloadAsJsonString.Should().Be(JsonConvert.SerializeObject(_customTypeRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_customTypeRequest)} but was {request.PayloadAsJsonString}");
                            _customTypeRequest.Should().BeEquivalentTo(request.GetPayload<CustomType>(), $"The expected respose payload should be {_customTypeRequest} but was {request.GetPayload<CustomType>()}");

                            methodCallReceived.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = _customTypeResponse
                        };

                        return Task.FromResult(response);
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_listPayloadAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        logger.Trace($"{nameof(SetDeviceReceiveMethod_listPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.PayloadAsJsonString.Should().Be(JsonConvert.SerializeObject(_listRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_listRequest)} but was {request.PayloadAsJsonString}");
                            _listRequest.Should().BeEquivalentTo(request.GetPayload<List<double>>(), $"The expected respose payload should be {_listRequest} but was {request.GetPayload<List<double>>()}");

                            methodCallReceived.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = _listResponse,
                        };

                        return Task.FromResult(response);
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_dictionaryPayloadAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        logger.Trace($"{nameof(SetDeviceReceiveMethod_dictionaryPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.PayloadAsJsonString.Should().Be(JsonConvert.SerializeObject(_dictRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_dictRequest)} but was {request.PayloadAsJsonString}");
                            _dictRequest.Should().BeEquivalentTo(request.GetPayload<Dictionary<string, object>>(), $"The expected respose payload should be {_arrayRequest} but was {request.GetPayload<Dictionary<string, object>>()}");

                            methodCallReceived.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = _dictResponse,
                        };

                        return Task.FromResult(response);
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethod_arrayPayloadAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        logger.Trace($"{nameof(SetDeviceReceiveMethod_arrayPayloadAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                        try
                        {
                            methodName.Should().Be(request.MethodName, $"The expected method name should be {methodName} but was {request.MethodName}");
                            request.PayloadAsJsonString.Should().Be(JsonConvert.SerializeObject(_arrayRequest), $"The expected respose payload should be {JsonConvert.SerializeObject(_arrayRequest)} but was {request.PayloadAsJsonString}");
                            _arrayRequest.Should().BeEquivalentTo(request.GetPayload<byte[]>(), $"The expected respose payload should be {_arrayRequest} but was {request.GetPayload<byte[]>()}");

                            methodCallReceived.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.SetException(ex);
                        }
                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = _arrayResponse,
                        };

                        return Task.FromResult(response);
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }
    }
}
