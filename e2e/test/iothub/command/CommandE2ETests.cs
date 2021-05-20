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
        public const string ComponentName = "testableComponent";

        private readonly string _devicePrefix = $"E2E_{nameof(CommandE2ETests)}_";
        private readonly string _modulePrefix = $"E2E_{nameof(CommandE2ETests)}_";
        private const string CommandName = "CommandE2ETest";

        private static readonly TimeSpan s_defaultCommandTimeoutMinutes = TimeSpan.FromMinutes(1);

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponse_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponse_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveCommandAsync).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribes_Mqtt()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_Tcp_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribes_MqttWs()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_WebSocket_Only, SubscribeAndUnsubscribeCommandAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponseWithComponent_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveCommandWithComponentAsync, withComponent: true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponseWithComponent_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveCommandWithComponentAsync, withComponent: true).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribesWithComponent_Mqtt()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_Tcp_Only, SubscribeAndUnsubscribeCommandWithComponentAsync, withComponent: true).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribesWithComponent_MqttWs()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_WebSocket_Only, SubscribeAndUnsubscribeCommandWithComponentAsync, withComponent: true).ConfigureAwait(false);
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
                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), 200));
                },
                null)
                .ConfigureAwait(false);

            await deviceClient.SubscribeToCommandsAsync(null, null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SubscribeAndUnsubscribeCommandWithComponentAsync(DeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SubscribeAndUnsubscribeCommandAsync)}: DeviceClient method: {request.ComponentName} - {request.CommandName}.");
                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), 200));
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

                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), 200));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveCommandWithComponentAsync(DeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveCommandAsync)}: DeviceClient method: {request.CommandName}.");

                    try
                    {
                        var valueToTest = request.GetData<ServiceCommandRequestObject>();
                        Assert.AreEqual(methodName, $"{request.ComponentName}*{request.CommandName}", $"The expected method name should be {methodName} but was {request.ComponentName}*{request.CommandName}");
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");
                        Assert.AreEqual(ServiceCommandRequestAssertion.a, valueToTest.a, $"The expected respose object did not decode properly. Value a should be {ServiceCommandRequestAssertion.a} but was {valueToTest?.a ?? int.MinValue}");
                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), 200));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        private async Task SendCommandAndUnsubscribeAsync(Client.TransportType transport, Func<DeviceClient, string, MsTestLogger, Task<Task>> subscribeAndUnsubscribeMethod, bool withComponent = false, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await subscribeAndUnsubscribeMethod(deviceClient, CommandName, Logger).ConfigureAwait(false);

            await ServiceSendCommandAndVerifyResponseAsync(testDevice.Id, CommandName, Logger, responseTimeout: responseTimeout, serviceClientTransportSettings: serviceClientTransportSettings).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendCommandAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod, bool withComponent = false, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            string commandNameString = CommandName;
            if (withComponent)
            {
                commandNameString = $"{ComponentName}*{CommandName}";
            }

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, commandNameString, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendCommandAndVerifyResponseAsync(testDevice.Id, commandNameString, DeviceResponseJson, ServiceRequestJson, Logger, responseTimeout, serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
