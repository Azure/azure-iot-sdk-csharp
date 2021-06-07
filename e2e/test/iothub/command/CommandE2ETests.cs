﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Commands
{
    public class ServiceCommandRequestAssertion
    {
        public int A => 123;
    }

    public class ServiceCommandRequestObject
    {
        public int A { get; set; }
    }

    public class DeviceCommandResponse
    {
        public string Name => "e2e_test";
    }

    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class CommandE2ETests : E2EMsTestBase
    {
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
        public async Task Command_DeviceReceivesCommandAndResponseWithComponent_Mqtt()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveCommandAsync, withComponent: true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Command_DeviceReceivesCommandAndResponseWithComponent_MqttWs()
        {
            await SendCommandAndRespondAsync(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveCommandAsync, withComponent: true).ConfigureAwait(false);
        }

        public static async Task DigitalTwinsSendCommandAndVerifyResponseAsync(string deviceId, string componentName, string commandName, MsTestLogger logger)
        {
            string payloadToSend = JsonConvert.SerializeObject(new ServiceCommandRequestObject { A = 123 });
            string responseExpected = JsonConvert.SerializeObject(new DeviceCommandResponse());
            string payloadReceived = string.Empty;
            int statusCode = 0;
#if NET451

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            logger.Trace($"{nameof(DigitalTwinsSendCommandAndVerifyResponseAsync)}: Invoke command {commandName}.");

            CloudToDeviceMethodResult serviceClientResponse = null;
            if (string.IsNullOrEmpty(componentName))
            {
                var serviceCommand = new CloudToDeviceMethod(commandName).SetPayloadJson(payloadToSend);
                serviceClientResponse =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId, serviceCommand).ConfigureAwait(false);
            }
            else
            {
                var serviceCommand = new CloudToDeviceMethod($"{componentName}*{commandName}").SetPayloadJson(payloadToSend);
                serviceClientResponse =
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceId, serviceCommand).ConfigureAwait(false);
            }

            statusCode = serviceClientResponse.Status;
            payloadReceived = serviceClientResponse.GetPayloadAsJson();

            serviceClient.Dispose();
#else
            DigitalTwinClient digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            logger.Trace($"{nameof(DigitalTwinsSendCommandAndVerifyResponseAsync)}: Invoke command {commandName}.");

            Rest.HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> response = null;
            if (string.IsNullOrEmpty(componentName))
            {
                response =
                await digitalTwinClient.InvokeCommandAsync(
                    deviceId,
                    commandName,
                    payloadToSend).ConfigureAwait(false);
            }
            else
            {
                response =
                await digitalTwinClient.InvokeComponentCommandAsync(
                    deviceId,
                    componentName,
                    commandName,
                    payloadToSend).ConfigureAwait(false);
            }

            statusCode = (int)response.Response.StatusCode;
            payloadReceived = response.Body.Payload;

            digitalTwinClient.Dispose();
#endif
            logger.Trace($"{nameof(DigitalTwinsSendCommandAndVerifyResponseAsync)}: Command status: {statusCode}.");
            Assert.AreEqual(200, statusCode, $"The expected response status should be 200 but was {statusCode}");
            Assert.AreEqual(responseExpected, payloadReceived, $"The expected response payload should be {responseExpected} but was {payloadReceived}");
        }

        public static async Task<Task> SetDeviceReceiveCommandAsync(DeviceClient deviceClient, string componentName, string commandName, MsTestLogger logger)
        {
            var commandCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SubscribeToCommandsAsync(
                (request, context) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveCommandAsync)}: DeviceClient command: {request.CommandName}.");

                    try
                    {
                        var valueToTest = request.GetData<ServiceCommandRequestObject>();
                        if (string.IsNullOrEmpty(componentName))
                        {
                            Assert.AreEqual(null, request.ComponentName, $"The expected component name should be null but was {request.ComponentName}");
                        }
                        else
                        {
                            Assert.AreEqual(componentName, request.ComponentName, $"The expected component name should be {componentName} but was {request.ComponentName}");
                        }
                        var assertionObject = new ServiceCommandRequestAssertion();
                        string responseExpected = JsonConvert.SerializeObject(assertionObject);
                        Assert.AreEqual(responseExpected, request.DataAsJson, $"The expected response payload should be {responseExpected} but was {request.DataAsJson}");
                        Assert.AreEqual(assertionObject.A, valueToTest.A, $"The expected response object did not decode properly. Value a should be {assertionObject.A} but was {valueToTest?.A ?? int.MinValue}");
                        commandCallReceived.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        commandCallReceived.SetException(ex);
                    }

                    return Task.FromResult(new CommandResponse(new DeviceCommandResponse(), 200));
                },
                null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return commandCallReceived.Task;
        }

        private async Task SendCommandAndRespondAsync(Client.TransportType transport, Func<DeviceClient, string, string, MsTestLogger, Task<Task>> setDeviceReceiveCommand, bool withComponent = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            string componentName = null;
            if (withComponent)
            {
                componentName = ComponentName;
            }

            Task commandReceivedTask = await setDeviceReceiveCommand(deviceClient, componentName, CommandName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    DigitalTwinsSendCommandAndVerifyResponseAsync(testDevice.Id, componentName, CommandName, Logger),
                    commandReceivedTask)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
