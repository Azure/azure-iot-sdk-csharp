// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
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

        private readonly string _devicePrefix = $"E2E_{nameof(DigitalTwinClientE2ETests)}_";
        private static readonly string s_connectionString = Configuration.IoTHub.ConnectionString;

        [LoggedTestMethod]
        public async Task DigitalTwinWithOnlyRootComponentOperationsAsync()
        {
            // Create a new test device instance.
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "Thermostat" model which has only a root component.
                var options = new ClientOptions
                {
                    ModelId = ThermostatModelId,
                };
                using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt, options);

                // Set callback to handle root command invocation request.
                int commandStatus = 200;
                string commandName = "getMaxMinReport";
                await deviceClient.SetMethodHandlerAsync(commandName,
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithOnlyRootComponentOperationsAsync)}: Digital twin command {request.Name} received.");
                        return Task.FromResult(new MethodResponse(commandStatus));
                    },
                    null);

                // Perform operations on the digital twin.
                using var digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(s_connectionString);

                // Retrieve the digital twin.
                Rest.HttpOperationResponse<ThermostatTwin, DigitalTwinGetHeaders> response =
                    await digitalTwinClient.GetAsync<ThermostatTwin>(deviceId).ConfigureAwait(false);
                ThermostatTwin twin = response.Body;
                twin.Metadata.ModelId.Should().Be(ThermostatModelId);

                // Invoke the root level command "getMaxMinReport" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                string payload = JsonConvert.SerializeObject(since);
                Rest.HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> commandResponse =
                    await digitalTwinClient.InvokeCommandAsync(deviceId, commandName, payload).ConfigureAwait(false);
                commandResponse.Body.Status.Should().Be(commandStatus);
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
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "TemperatureController" model which has "Thermostat" components.
                var options = new ClientOptions
                {
                    ModelId = TemperatureControllerModelId,
                };
                using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt, options);

                // Set callback handler for receiving twin property updates.
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
                {
                    Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: DesiredProperty update received: {patch}, {context}");
                    return Task.FromResult(true);
                }, deviceClient);

                // Set callbacks to handle command requests.
                int commandStatus = 200;

                // Set callback to handle root command invocation request.
                string rootCommandName = "reboot";
                await deviceClient.SetMethodHandlerAsync(rootCommandName,
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: Digital twin command {request.Name} received.");
                        return Task.FromResult(new MethodResponse(commandStatus));
                    },
                    null);

                // Set callback to handle component command invocation request.
                string componentName = "thermostat1";
                string componentCommandName = "getMaxMinReport";
                await deviceClient.SetMethodHandlerAsync($"{componentName}*{componentCommandName}",
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: Digital twin command {request.Name} received.");
                        return Task.FromResult(new MethodResponse(commandStatus));
                    },
                    null);

                // Perform operations on the digital twin
                using var digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(s_connectionString);

                // Retrieve the digital twin.
                Rest.HttpOperationResponse<TemperatureControllerTwin, DigitalTwinGetHeaders> response =
                    await digitalTwinClient.GetAsync<TemperatureControllerTwin>(deviceId).ConfigureAwait(false);
                TemperatureControllerTwin twin = response.Body;
                twin.Metadata.ModelId.Should().Be(TemperatureControllerModelId);

                // Invoke the root level command "reboot" on the digital twin.
                int delay = 1;
                string rootCommandPayload = JsonConvert.SerializeObject(delay);
                Rest.HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> rootCommandResponse =
                    await digitalTwinClient.InvokeCommandAsync(deviceId, rootCommandName, rootCommandPayload).ConfigureAwait(false);
                rootCommandResponse.Body.Status.Should().Be(commandStatus);

                // Invoke the root level command "getMaxMinReport" under component "thermostat1" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                string componentCommandPayload = JsonConvert.SerializeObject(since);
                Rest.HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> componentCommandResponse =
                    await digitalTwinClient.InvokeComponentCommandAsync(deviceId, componentName, componentCommandName, componentCommandPayload).ConfigureAwait(false);
                componentCommandResponse.Body.Status.Should().Be(commandStatus);
            }
            finally
            {
                // Delete the device.
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }
    }
}
