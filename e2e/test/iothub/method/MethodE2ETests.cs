// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(TransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_MqttTcp()
        {
            await SendMethodAndUnsubscribeAsync(new MqttTransportSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_MqttWs()
        {
            await SendMethodAndUnsubscribeAsync(new MqttTransportSettings(TransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttTcp()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(), SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(TransportProtocol.WebSocket), SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(TransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_AmqpTcp()
        {
            await SendMethodAndUnsubscribeAsync(new AmqpTransportSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_AmqpWs()
        {
            await SendMethodAndUnsubscribeAsync(new AmqpTransportSettings(TransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(), SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(TransportProtocol.WebSocket), SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceSendsMethodThroughProxyWithDefaultTimeout()
        {
            var serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    new MqttTransportSettings(),
                    SetDeviceReceiveMethodAsync,
                    serviceClientTransportSettings: serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceSendsMethodThroughProxyWithCustomTimeout()
        {
            var serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    new MqttTransportSettings(),
                    SetDeviceReceiveMethodAsync,
                    TimeSpan.FromMinutes(5),
                    serviceClientTransportSettings)
            .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownDeviceThrows()
        {
            // setup
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
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

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(TransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_MqttTcp()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(), SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(new MqttTransportSettings(TransportProtocol.WebSocket), SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(TransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(), SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ModuleReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(new AmqpTransportSettings(TransportProtocol.WebSocket), SetModuleReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownModuleThrows()
        {
            // setup
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, "ModuleNotFoundTest").ConfigureAwait(false);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
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

        [LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // arrange

            IotHubDeviceClient deviceClient = null;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, "NullMethodPayloadTest").ConfigureAwait(false);

            try
            {
                const string commandName = "Reboot";
                bool deviceMethodCalledSuccessfully = false;

                deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new MqttTransportSettings()));

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

                using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
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
                deviceClient.Dispose();

                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
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
                serviceClient.Dispose();
            }
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string methodName,
            string respJson,
            string reqJson,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    new CloudToDeviceMethod(methodName, methodTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
            serviceClient.Dispose();
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string moduleId,
            string methodName,
            string respJson,
            string reqJson,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = serviceClientTransportSettings == default
                ? ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString)
                : ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    moduleId,
                    new CloudToDeviceMethod(methodName, responseTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
            serviceClient.Dispose();
        }

        public static async Task<Task> SubscribeAndUnsubscribeMethodAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SetMethodHandlerAsync(
                methodName,
                (request, context) =>
                {
                    logger.Trace($"{nameof(SubscribeAndUnsubscribeMethodAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null)
                .ConfigureAwait(false);

            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethodAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SetMethodHandlerAsync(
                    methodName,
                    (request, context) =>
                        {
                            logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

                            try
                            {
                                Assert.AreEqual(methodName, request.Name, $"The expected method name should be {methodName} but was {request.Name}");
                                Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");

                                methodCallReceived.SetResult(true);
                            }
                            catch (Exception ex)
                            {
                                methodCallReceived.SetException(ex);
                            }

                            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                        },
                    null)
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethodDefaultHandlerAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SetMethodDefaultHandlerAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveMethodDefaultHandlerAsync)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

                    try
                    {
                        Assert.AreEqual(methodName, request.Name, $"The expected method name should be {methodName} but was {request.Name}");
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");

                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveMethodAsync(IotHubModuleClient moduleClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await moduleClient.SetMethodHandlerAsync(
                methodName,
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

                    try
                    {
                        Assert.AreEqual(methodName, request.Name, $"The expected method name should be {methodName} but was {request.Name}");
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");

                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveMethodDefaultHandlerAsync(IotHubModuleClient moduleClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await moduleClient.SetMethodDefaultHandlerAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveMethodDefaultHandlerAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

                    try
                    {
                        Assert.AreEqual(methodName, request.Name, $"The expected method name should be {methodName} but was {request.Name}");
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");

                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            return methodCallReceived.Task;
        }

        private async Task SendMethodAndUnsubscribeAsync(
            ITransportSettings transportSettings,
            Func<IotHubDeviceClient,
                string, MsTestLogger,
                Task<Task>> subscribeAndUnsubscribeMethod,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            await subscribeAndUnsubscribeMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyNotReceivedAsync(testDevice.Id, MethodName, Logger, responseTimeout, serviceClientTransportSettings).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            ITransportSettings transportSettings,
            Func<IotHubDeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                DeviceResponseJson,
                ServiceRequestJson,
                Logger,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            ITransportSettings transportSettings,
            Func<IotHubModuleClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var moduleClient = IotHubModuleClient.CreateFromConnectionString(testModule.ConnectionString, options);

            Task methodReceivedTask = await setDeviceReceiveMethod(moduleClient, MethodName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendMethodAndVerifyResponseAsync(
                        testModule.DeviceId,
                        testModule.Id,
                        MethodName,
                        DeviceResponseJson,
                        ServiceRequestJson,
                        Logger,
                        responseTimeout,
                        serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await moduleClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
