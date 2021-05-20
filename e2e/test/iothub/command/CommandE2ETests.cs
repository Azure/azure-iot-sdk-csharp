// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET451
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
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveCommandAsync, withComponent: true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponseWithComponent_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveCommandAsync, withComponent: true).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribesWithComponent_Mqtt()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_Tcp_Only, SubscribeAndUnsubscribeCommandAsync, withComponent: true).ConfigureAwait(false);
        }


        [LoggedTestMethod]
        public async Task Command_DeviceUnsubscribesWithComponent_MqttWs()
        {
            await SendCommandAndUnsubscribeAsync(Client.TransportType.Mqtt_WebSocket_Only, SubscribeAndUnsubscribeCommandAsync, withComponent: true).ConfigureAwait(false);
        }

        public static async Task DigitalTwinsSendCommandAndVerifyResponseAsync(string deviceId, string componentName, string methodName, string respJson, string reqJson, MsTestLogger logger)
        {
            DigitalTwinClient digitalTwinClient = null;
            digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            logger.Trace($"{nameof(DigitalTwinsSendCommandAndVerifyResponseAsync)}: Invoke method {methodName}.");
            Rest.HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> response = null;
            if (string.IsNullOrEmpty(componentName))
            {
                response =
                await digitalTwinClient.InvokeCommandAsync(
                    deviceId,
                    methodName,
                    reqJson).ConfigureAwait(false);
            }
            else
            {
                response =
                await digitalTwinClient.InvokeComponentCommandAsync(
                    deviceId,
                    componentName,
                    methodName,
                    reqJson).ConfigureAwait(false);
            }


            logger.Trace($"{nameof(DigitalTwinsSendCommandAndVerifyResponseAsync)}: Method status: {response.Response.StatusCode}.");
            Assert.AreEqual(200, Shared.StatusCodes.OK, $"The expected response status should be 200 but was {response.Response.StatusCode}");
            string payload = response.Body.Payload;
            Assert.AreEqual(respJson, payload, $"The expected response payload should be {respJson} but was {payload}");

            digitalTwinClient.Dispose();
        }

        public static async Task<Task> SubscribeAndUnsubscribeCommandAsync(DeviceClient deviceClient, string componentName, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SubscribeToCommandsAsync(
                (request, context) =>
                {
                    if (string.IsNullOrEmpty(componentName))
                    {
                        logger.Trace($"{nameof(SubscribeAndUnsubscribeCommandAsync)}: DeviceClient method: {request.CommandName}.");
                    }
                    else
                    {
                        logger.Trace($"{nameof(SubscribeAndUnsubscribeCommandAsync)}: DeviceClient method: {request.ComponentName} {request.CommandName}.");
                    }
                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), Shared.StatusCodes.OK));
                },
                null)
                .ConfigureAwait(false);

            await deviceClient.SubscribeToCommandsAsync(null, null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetDeviceReceiveCommandAsync(DeviceClient deviceClient, string componentName, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveCommandAsync)}: DeviceClient method: {request.CommandName}.");

                    try
                    {
                        var valueToTest = request.GetData<ServiceCommandRequestObject>();
                        if (!string.IsNullOrEmpty(componentName))
                        {
                            Assert.AreEqual(componentName, request.ComponentName, $"The expected component name should be {componentName} but was {request.ComponentName}");
                        } else
                        {
                            Assert.AreEqual(null, request.ComponentName, $"The expected component name should be null but was {request.ComponentName}");
                        }
                        
                        Assert.AreEqual(ServiceRequestJson, request.DataAsJson, $"The expected respose payload should be {ServiceRequestJson} but was {request.DataAsJson}");
                        Assert.AreEqual(ServiceCommandRequestAssertion.a, valueToTest.a, $"The expected respose object did not decode properly. Value a should be {ServiceCommandRequestAssertion.a} but was {valueToTest?.a ?? int.MinValue}");
                        methodCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        methodCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), Shared.StatusCodes.OK));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        private async Task SendCommandAndUnsubscribeAsync(Client.TransportType transport, Func<DeviceClient, string, string, MsTestLogger, Task<Task>> subscribeAndUnsubscribeMethod, bool withComponent = false, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            string componentName = null;
            if (withComponent)
            {
                componentName = ComponentName;
            }

            await subscribeAndUnsubscribeMethod(deviceClient, componentName, CommandName, Logger).ConfigureAwait(false);

            await SubscribeAndUnsubscribeCommandAsync(deviceClient, componentName, CommandName, Logger);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendCommandAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod, bool withComponent = false, TimeSpan responseTimeout = default, ServiceClientTransportSettings serviceClientTransportSettings = default)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            string componentName = null;
            if (withComponent)
            {
                componentName = ComponentName;
            }

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, componentName, CommandName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    DigitalTwinsSendCommandAndVerifyResponseAsync(testDevice.Id, componentName, CommandName, DeviceResponseJson, ServiceRequestJson, Logger),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
#endif