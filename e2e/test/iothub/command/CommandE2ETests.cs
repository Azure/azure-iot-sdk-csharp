 // Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Commands
{
    public static class ServiceCommandRequestAssertion
    {
        public static int a => 123;
    }

    public class ServiceCommandRequestObject
    {
        public int a { get; set; }
    }

    public class DeviceCommandResponse
    {
        public string name => "e2e_test";
    }

    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class CommandE2ETests : E2EMsTestBase
    {
        public const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        public const string ServiceRequestJson = "{\"a\":123}";

        private readonly string _devicePrefix = $"E2E_{nameof(CommandE2ETests)}_";
        private readonly string _modulePrefix = $"E2E_{nameof(CommandE2ETests)}_";
        private const string CommandName = "CommandE2ETest";

        private static readonly TimeSpan s_defaultCommandTimeoutMinutes = TimeSpan.FromMinutes(1);

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesCommandAndResponse_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_Mqtt()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_Tcp_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_MqttWs()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_WebSocket_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_DeviceReceivesCommandAndResponse_Amqp()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_DeviceReceivesCommandAndResponse_AmqpWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_Amqp()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Amqp_Tcp_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_DeviceUnsubscribes_AmqpWs()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Amqp_WebSocket_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }


        //[LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceCommandWithUnknownDeviceThrows()
        {
            // setup
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
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

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponse_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetModuleReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponse_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetModuleReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetModuleReceiveCommandDefaultHandlerAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetModuleReceiveCommandDefaultHandlerAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponse_Amqp()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetModuleReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponse_AmqpWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetModuleReceiveCommandAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetModuleReceiveCommandDefaultHandlerAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ModuleReceivesCommandAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetModuleReceiveCommandDefaultHandlerAsync).ConfigureAwait(false);
        }

        //[LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceCommandWithUnknownModuleThrows()
        {
            // setup
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, "ModuleNotFoundTest").ConfigureAwait(false);
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
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

        public static async Task ServiceSendCommandAndVerifyResponseAsync(string deviceId, string methodName, MsTestLogger logger, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = null;
            if (serviceClientTransportSettings == default)
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            }
            else
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);
            }

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultCommandTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendCommandAndVerifyResponseAsync)}: Invoke method {methodName}.");
            try
            {
                CloudToDeviceMethodResult response =
                    await serviceClient.InvokeDeviceMethodAsync(
                        deviceId,
                        new CloudToDeviceMethod(methodName, methodTimeout).SetPayloadJson(null)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!(ex is DeviceNotFoundException))
                    throw ex;
            }
            finally
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();
            }
        }

        public static async Task ServiceSendCommandAndVerifyResponseAsync(string deviceId, string methodName, string respJson, string reqJson, MsTestLogger logger, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = null;
            if (serviceClientTransportSettings == default)
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            }
            else
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);
            }

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultCommandTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendCommandAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    new CloudToDeviceMethod(methodName, methodTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendCommandAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
            serviceClient.Dispose();
        }

        public static async Task ServiceSendCommandAndVerifyResponseAsync(string deviceId, string moduleId, string methodName, string respJson, string reqJson, MsTestLogger logger, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            ServiceClient serviceClient = null;
            if (serviceClientTransportSettings == default)
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            }
            else
            {
                serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);
            }

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultCommandTimeoutMinutes : responseTimeout;

            logger.Trace($"{nameof(ServiceSendCommandAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId,
                    moduleId,
                    new CloudToDeviceMethod(methodName, responseTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendCommandAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
            serviceClient.Dispose();
        }

        public static async Task<Task> SubscribeAndUnsubscribeCommandAsync(DeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SubscribeAndUnsubscribeCommandAsync)}: DeviceClient method: {request.CommandName}.");
                    return Task.FromResult(new Shared.CommandResponse(new DeviceCommandResponse(), 200));
                },
                null)
                .ConfigureAwait(false);

            await deviceClient.SubscribeToCommandsAsync(null, null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveCommandAsync(DeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveCommandAsync)}: DeviceClient method: {request.CommandName}.");

                    try
                    {
                        var valueToTest = request.GetData<ServiceCommandRequestObject>();
                        Assert.AreEqual(methodName, request.CommandName, $"The expected method name should be {methodName} but was {request.CommandName}");
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");
                        Assert.AreEqual(ServiceCommandRequestAssertion.a, valueToTest.a, $"The expected respose object did not decode properly. Value a should be {ServiceCommandRequestAssertion.a} but was {valueToTest?.a ?? int.MinValue}");
                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new Shared.CommandResponse(new DeviceCommandResponse(), 200));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveCommandAsync(ModuleClient moduleClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await moduleClient.SetMethodHandlerAsync(methodName,
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetModuleReceiveCommandAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

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

        public static async Task<Task> SetModuleReceiveCommandDefaultHandlerAsync(ModuleClient moduleClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await moduleClient.SetMethodDefaultHandlerAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetModuleReceiveCommandDefaultHandlerAsync)}: ModuleClient method: {request.Name} {request.ResponseTimeout}.");

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

        private async Task SendCommandAndUnsubscribeAsync(Client.TransportType transport, Func<DeviceClient, string, MsTestLogger, Task<Task>> subscribeAndUnsubscribeMethod, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await subscribeAndUnsubscribeMethod(deviceClient, CommandName, Logger).ConfigureAwait(false);

            await ServiceSendCommandAndVerifyResponseAsync(testDevice.Id, CommandName, Logger, responseTimeout, serviceClientTransportSettings).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendCommandAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, CommandName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendCommandAndVerifyResponseAsync(testDevice.Id, CommandName, DeviceResponseJson, ServiceRequestJson, Logger, responseTimeout, serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendCommandAndRespondAsync(Client.TransportType transport, Func<ModuleClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transport);

            Task methodReceivedTask = await setDeviceReceiveMethod(moduleClient, CommandName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendCommandAndVerifyResponseAsync(testModule.DeviceId, testModule.Id, CommandName, DeviceResponseJson, ServiceRequestJson, Logger, responseTimeout, serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await moduleClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
