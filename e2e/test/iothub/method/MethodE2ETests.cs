// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MethodE2ETests : E2EMsTestBase
    {
        public const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        public const string ServiceRequestJson = "{\"a\":123}";

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private readonly string _modulePrefix = $"{nameof(MethodE2ETests)}_mod_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodTimeoutMinutes = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_Mqtt()
        {
            await SendMethodAndUnsubscribeAsync(Client.TransportType.Mqtt_Tcp_Only, SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_MqttWs()
        {
            await SendMethodAndUnsubscribeAsync(Client.TransportType.Mqtt_WebSocket_Only, SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_Amqp()
        {
            await SendMethodAndUnsubscribeAsync(Client.TransportType.Amqp_Tcp_Only, SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_AmqpWs()
        {
            await SendMethodAndUnsubscribeAsync(Client.TransportType.Amqp_WebSocket_Only, SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceSendsMethodThroughProxyWithDefaultTimeout()
        {
            var serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetDeviceReceiveMethodAsync,
                    serviceClientTransportSettings: serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceSendsMethodThroughProxyWithCustomTimeout()
        {
            var serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetDeviceReceiveMethodAsync,
                    TimeSpan.FromMinutes(5),
                    serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownDeviceThrows()
        {
            // setup
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval");
            methodInvocation.SetPayloadJson("10");

            // act
            ErrorCode actualErrorCode = ErrorCode.InvalidErrorCode;
            try
            {
                // Invoke the direct method asynchronously and get the response from the simulated device.
                await serviceClient.InvokeDeviceMethodAsync("SomeNonExistantDevice", methodInvocation);
            }
            catch (DeviceNotFoundException ex)
            {
                actualErrorCode = ex.Code;
            }

            Assert.AreEqual(ErrorCode.DeviceNotFound, actualErrorCode);

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownModuleThrows()
        {
            // setup
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync("ModuleNotFoundTest").ConfigureAwait(false);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval");
            methodInvocation.SetPayloadJson("10");

            // act
            ErrorCode actualErrorCode = ErrorCode.InvalidErrorCode;
            try
            {
                // Invoke the direct method asynchronously and get the response from the simulated device.
                await serviceClient.InvokeDeviceMethodAsync(testDevice.Id, "someNonExistantModuleOnAnExistingDevice", methodInvocation).ConfigureAwait(false);
            }
            catch (DeviceNotFoundException ex)
            {
                // Although the exception is called "Device" not found, it is used for all 404's, including the 404010 that denotes a module was not found
                actualErrorCode = ex.Code;
            }

            Assert.AreEqual(ErrorCode.ModuleNotFound, actualErrorCode);

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // arrange

            const string commandName = "Reboot";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync("NullMethodPayloadTest").ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt);

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient
                    .SetMethodDefaultHandlerAsync(
                        (methodRequest, userContext) =>
                        {
                            methodRequest.Name.Should().Be(commandName);
                            deviceMethodCalledSuccessfully = true;
                            return Task.FromResult(new MethodResponse(200));
                        },
                        null)
                    .ConfigureAwait(false);

                using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
                CloudToDeviceMethod c2dMethod = new CloudToDeviceMethod(commandName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).SetPayloadJson(null);

                // act

                CloudToDeviceMethodResult result = await serviceClient.InvokeDeviceMethodAsync(testDevice.Id, c2dMethod).ConfigureAwait(false);

                // assert

                deviceMethodCalledSuccessfully.Should().BeTrue();
            }
            finally
            {
                // clean up

                await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithDateTimePayload_DoesNotThrow()
        {
            // arrange

            var date = new DateTimeOffset(638107582284599400, TimeSpan.FromHours(1));

            string responseJson = JsonConvert.SerializeObject(new TestDateTime { Iso8601String = date.ToString("o", CultureInfo.InvariantCulture) });
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

            const string commandName = "GetDateTime";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync("DateTimeMethodPayloadTest").ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt);

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient
                    .SetMethodHandlerAsync(
                        commandName,
                        (methodRequest, userContext) =>
                        {
                            methodRequest.Name.Should().Be(commandName);
                            deviceMethodCalledSuccessfully = true;
                            return Task.FromResult(new MethodResponse(responseBytes, 200));
                        },
                        null)
                    .ConfigureAwait(false);

                using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
                CloudToDeviceMethod c2dMethod = new CloudToDeviceMethod(commandName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).SetPayloadJson(null);

                // act

                CloudToDeviceMethodResult result = await serviceClient.InvokeDeviceMethodAsync(testDevice.Id, c2dMethod).ConfigureAwait(false);
                string actualResultJson = result.GetPayloadAsJson();
                TestDateTime myDtoValue = JsonConvert.DeserializeObject<TestDateTime>(actualResultJson);
                string value = myDtoValue.Iso8601String;

                Action act = () => DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                // assert

                deviceMethodCalledSuccessfully.Should().BeTrue();
                responseJson.Should().Be(actualResultJson);
                act.Should().NotThrow();
            }
            finally
            {
                // clean up

                await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            try
            {
                CloudToDeviceMethodResult response = await serviceClient
                    .InvokeDeviceMethodAsync(
                        deviceId,
                        new CloudToDeviceMethod(methodName, methodTimeout).SetPayloadJson(null))
                    .ConfigureAwait(false);
            }
            catch (DeviceNotFoundException)
            {
            }
            finally
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string methodName,
            string respJson,
            string reqJson,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response = await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    new CloudToDeviceMethod(methodName, methodTimeout).SetPayloadJson(reqJson))
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string moduleId,
            string methodName,
            string respJson,
            string reqJson,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    moduleId,
                    new CloudToDeviceMethod(methodName, responseTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task<Task> SubscribeAndUnsubscribeMethodAsync(DeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
                );

            await deviceClient
                .SetMethodHandlerAsync(
                    methodName,
                    (request, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SubscribeAndUnsubscribeMethodAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");
                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                    },
                    null)
                .ConfigureAwait(false);

            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethodAsync(DeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
                );

            await deviceClient
                .SetMethodHandlerAsync(
                    methodName,
                    (request, context) =>
                        {
                            VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

                            try
                            {
                                request.Name.Should().Be(methodName);
                                request.DataAsJson.Should().Be(ServiceRequestJson);

                                methodCallReceived.TrySetResult(true);
                            }
                            catch (Exception ex)
                            {
                                methodCallReceived.TrySetException(ex);
                            }

                            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                        },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethodDefaultHandlerAsync(DeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
                );

            await deviceClient
                .SetMethodDefaultHandlerAsync(
                    (request, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodDefaultHandlerAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

                        try
                        {
                            request.Name.Should().Be(methodName);
                            request.DataAsJson.Should().Be(ServiceRequestJson);

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }

                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                    },
                    null)
                .ConfigureAwait(false);

            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveMethodAsync(ModuleClient moduleClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
                );

            await moduleClient.SetMethodHandlerAsync(
                    methodName,
                    (request, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

                        try
                        {
                            request.Name.Should().Be(methodName);
                            request.DataAsJson.Should().Be(ServiceRequestJson);

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }

                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                    },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveMethodDefaultHandlerAsync(ModuleClient moduleClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
                );

            await moduleClient.SetMethodDefaultHandlerAsync(
                    (request, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodDefaultHandlerAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

                        try
                        {
                            request.Name.Should().Be(methodName);
                            request.DataAsJson.Should().Be(ServiceRequestJson);

                            methodCallReceived.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }

                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                    },
                    null)
                .ConfigureAwait(false);

            return methodCallReceived.Task;
        }

        private async Task SendMethodAndUnsubscribeAsync(
            Client.TransportType transport,
            Func<DeviceClient,
                string,
                Task<Task>> subscribeAndUnsubscribeMethod,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await subscribeAndUnsubscribeMethod(deviceClient, MethodName).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyNotReceivedAsync(
                    testDevice.Id,
                    MethodName,
                    responseTimeout,
                    serviceClientTransportSettings)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            Client.TransportType transport,
            Func<DeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                DeviceResponseJson,
                ServiceRequestJson,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(Client.TransportType transport, Func<ModuleClient, string, Task<Task>> setDeviceReceiveMethod, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix).ConfigureAwait(false);
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transport);

            Task methodReceivedTask = await setDeviceReceiveMethod(moduleClient, MethodName).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendMethodAndVerifyResponseAsync(
                        testModule.DeviceId,
                        testModule.Id,
                        MethodName,
                        DeviceResponseJson,
                        ServiceRequestJson,
                        responseTimeout,
                        serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await moduleClient.CloseAsync().ConfigureAwait(false);
        }

        private class TestDateTime
        {
            public string Iso8601String { get; set; }
        }
    }
}
