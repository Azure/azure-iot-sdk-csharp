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

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MethodE2ETests : E2EMsTestBase
    {
        public const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        public const string ServiceRequestJson = "{\"a\":123}";

        private readonly string _devicePrefix = $"E2E_{nameof(MethodE2ETests)}_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodTimeoutMinutes = TimeSpan.FromMinutes(1);

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespondAsync(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodDefaultHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceSendsMethodThroughProxyWithDefaultTimeout()
        {
            ServiceClientTransportSettings serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodAsync, serviceClientTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceSendsMethodThroughProxyWithCustomTimeout()
        {
            ServiceClientTransportSettings serviceClientTransportSettings = new ServiceClientTransportSettings
            {
                HttpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodAsync, TimeSpan.FromMinutes(5), serviceClientTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownDeviceThrows()
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
                await serviceClient.InvokeDeviceMethodAsync("MyDummyDevice", methodInvocation);
            }
            catch (DeviceNotFoundException ex)
            {
                actualErrorCode = ex.Code;
            }

            Assert.AreEqual(ErrorCode.DeviceNotFound, actualErrorCode);

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(string deviceName, string methodName, string respJson, string reqJson, TestLogger logger)
        {
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceName,
                    new CloudToDeviceMethod(methodName, s_defaultMethodTimeoutMinutes).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(string deviceName, string methodName, string respJson, string reqJson, TimeSpan responseTimeout, ServiceClientTransportSettings serviceClientTransportSettings, TestLogger logger)
        {
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp, serviceClientTransportSettings);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceName,
                    new CloudToDeviceMethod(methodName, responseTimeout).SetPayloadJson(reqJson)).ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status, $"The expected response status should be 200 but was {response.Status}");
            string payload = response.GetPayloadAsJson();
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task<Task> SetDeviceReceiveMethodAsync(DeviceClient deviceClient, string methodName, TestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SetMethodHandlerAsync(methodName,
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
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveMethodDefaultHandlerAsync(DeviceClient deviceClient, string methodName, TestLogger logger)
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

        private static Task<Task> SetDeviceReceiveMethodObsoleteHandler(DeviceClient deviceClient, string methodName, TestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

#pragma warning disable CS0618
            deviceClient.SetMethodHandler(methodName, (request, context) =>
            {
                logger.Trace($"{nameof(SetDeviceReceiveMethodObsoleteHandler)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

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
            }, null);
#pragma warning restore CS0618

            return Task.FromResult<Task>(methodCallReceived.Task);
        }

        private async Task SendMethodAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, TestLogger, Task<Task>> setDeviceReceiveMethod)
        {
            await SendMethodAndRespondAsync(transport, setDeviceReceiveMethod, new ServiceClientTransportSettings()).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, TestLogger, Task<Task>> setDeviceReceiveMethod, ServiceClientTransportSettings serviceClientTransportSettings)
        {
            await SendMethodAndRespondAsync(transport, setDeviceReceiveMethod, s_defaultMethodTimeoutMinutes, serviceClientTransportSettings).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, TestLogger, Task<Task>> setDeviceReceiveMethod, TimeSpan responseTimeout, ServiceClientTransportSettings serviceClientTransportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson, responseTimeout, serviceClientTransportSettings, Logger),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
