// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Serialization;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DigitalTwinClientE2ETests : E2EMsTestBase
    {
        private const string ThermostatModelId = "dtmi:com:example:Thermostat;1";
        private const string TemperatureControllerModelId = "dtmi:com:example:TemperatureController;1";

        private readonly string _devicePrefix = $"{nameof(DigitalTwinClientE2ETests)}_";
        private static readonly string s_connectionString = TestConfiguration.IoTHub.ConnectionString;

        [LoggedTestMethod]
        public async Task DigitalTwinWithOnlyRootComponentOperationsAsync()
        {
            // Create a new test device instance.
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "Thermostat" model which has only a root component.
                var options = new ClientOptions(new MqttTransportSettings())
                {
                    ModelId = ThermostatModelId,
                };
                using DeviceClient deviceClient = testDevice.CreateDeviceClient(options);

                // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Perform operations on the digital twin.
                using var digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(s_connectionString);

                // Retrieve the digital twin.
                HttpOperationResponse<ThermostatTwin, DigitalTwinGetHeaders> response =
                    await digitalTwinClient.GetDigitalTwinAsync<ThermostatTwin>(deviceId).ConfigureAwait(false);
                ThermostatTwin twin = response.Body;
                twin.Metadata.ModelId.Should().Be(ThermostatModelId);

                // Set callback handler for receiving root-level twin property updates.
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
                {
                    Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: DesiredProperty update received: {patch}, {context}");
                    return Task.FromResult(true);
                }, deviceClient);

                // Update the root-level property "targetTemperature".
                string propertyName = "targetTemperature";
                double propertyValue = new Random().Next(0, 100);
                var ops = new UpdateOperationsUtility();
                ops.AppendAddPropertyOp($"/{propertyName}", propertyValue);
                string patch = ops.Serialize();
                HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateResponse =
                    await digitalTwinClient.UpdateDigitalTwinAsync(deviceId, patch);
                updateResponse.Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

                // Set callback to handle root-level command invocation request.
                int expectedCommandStatus = 200;
                string commandName = "getMaxMinReport";
                await deviceClient.SetMethodHandlerAsync(commandName,
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithOnlyRootComponentOperationsAsync)}: Digital twin command received: {request.Name}.");
                        string payload = JsonConvert.SerializeObject(request.Name);
                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(payload), expectedCommandStatus));
                    },
                    null);

                // Invoke the root-level command "getMaxMinReport" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                string payload = JsonConvert.SerializeObject(since);
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> commandResponse =
                    await digitalTwinClient.InvokeCommandAsync(deviceId, commandName, payload).ConfigureAwait(false);
                commandResponse.Body.Status.Should().Be(expectedCommandStatus);
                commandResponse.Body.Payload.Should().Be(JsonConvert.SerializeObject(commandName));
            }
            finally
            {
                // Delete the device.
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task DigitalTwinWithComponentOperationsAsync()
        {
            // Create a new test device instance.
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "TemperatureController" model which has "Thermostat" components.
                var options = new ClientOptions
                {
                    ModelId = TemperatureControllerModelId,
                };
                using DeviceClient deviceClient = testDevice.CreateDeviceClient(options);

                // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Perform operations on the digital twin.
                using var digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(s_connectionString);

                // Retrieve the digital twin.
                HttpOperationResponse<TemperatureControllerTwin, DigitalTwinGetHeaders> response =
                    await digitalTwinClient.GetDigitalTwinAsync<TemperatureControllerTwin>(deviceId).ConfigureAwait(false);
                TemperatureControllerTwin twin = response.Body;
                twin.Metadata.ModelId.Should().Be(TemperatureControllerModelId);

                string componentName = "thermostat1";

                // Set callback handler for receiving twin property updates.
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
                {
                    Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: DesiredProperty update received: {patch}, {context}");
                    return Task.FromResult(true);
                }, deviceClient);

                // Update the property "targetTemperature" under component "thermostat1" on the digital twin.
                // NOTE: since this is the first operation on the digital twin, the component "thermostat1" doesn't exist on it yet.
                // So we will create a property patch that "adds" a component, and updates the property in it.
                string propertyName = "targetTemperature";
                double propertyValue = new Random().Next(0, 100);
                var propertyValues = new Dictionary<string, object> { { propertyName, propertyValue } };

                var ops = new UpdateOperationsUtility();
                ops.AppendAddComponentOp($"/{componentName}", propertyValues);
                string patch = ops.Serialize();
                HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateResponse =
                    await digitalTwinClient.UpdateDigitalTwinAsync(deviceId, patch);
                updateResponse.Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

                // Set callbacks to handle command requests.
                int expectedCommandStatus = 200;

                // Set callback to handle root-level command invocation request.
                string rootCommandName = "reboot";
                await deviceClient.SetMethodHandlerAsync(rootCommandName,
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: Digital twin command {request.Name} received.");
                        string payload = JsonConvert.SerializeObject(request.Name);
                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(payload), expectedCommandStatus));
                    },
                    null);

                // Invoke the root-level command "reboot" on the digital twin.
                int delay = 1;
                string rootCommandPayload = JsonConvert.SerializeObject(delay);
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> rootCommandResponse =
                    await digitalTwinClient.InvokeCommandAsync(deviceId, rootCommandName, rootCommandPayload).ConfigureAwait(false);
                rootCommandResponse.Body.Status.Should().Be(expectedCommandStatus);
                rootCommandResponse.Body.Payload.Should().Be(JsonConvert.SerializeObject(rootCommandName));

                // Set callback to handle component-level command invocation request.
                // For a component-level command, the command name is in the format "<component-name>*<command-name>".
                string componentCommandName = "getMaxMinReport";
                string componentCommandNamePnp = $"{componentName}*{componentCommandName}";
                await deviceClient.SetMethodHandlerAsync(componentCommandNamePnp,
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: Digital twin command {request.Name} received.");
                        string payload = JsonConvert.SerializeObject(request.Name);
                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(payload), expectedCommandStatus));
                    },
                    null);

                // Invoke the command "getMaxMinReport" under component "thermostat1" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                string componentCommandPayload = JsonConvert.SerializeObject(since);
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> componentCommandResponse =
                    await digitalTwinClient.InvokeComponentCommandAsync(deviceId, componentName, componentCommandName, componentCommandPayload).ConfigureAwait(false);
                componentCommandResponse.Body.Status.Should().Be(expectedCommandStatus);
                componentCommandResponse.Body.Payload.Should().Be(JsonConvert.SerializeObject(componentCommandNamePnp));
            }
            finally
            {
                // Delete the device.
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }
    }
}
