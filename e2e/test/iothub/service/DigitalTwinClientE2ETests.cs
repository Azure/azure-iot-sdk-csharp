// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DigitalTwinClientE2ETests : E2EMsTestBase
    {
        private const string ThermostatModelId = "dtmi:com:example:Thermostat;1";
        private const string TemperatureControllerModelId = "dtmi:com:example:TemperatureController;1";

        private readonly string _devicePrefix = $"{nameof(DigitalTwinClientE2ETests)}_";
        private static readonly string s_connectionString = TestConfiguration.IotHub.ConnectionString;

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DigitalTwinWithOnlyRootComponentOperationsAsync()
        {
            // Create a new test device instance.
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "Thermostat" model which has only a root component.
                var options = new IotHubClientOptions(new IotHubClientMqttSettings())
                {
                    ModelId = ThermostatModelId,
                };
                using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

                // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Perform operations on the digital twin.
                using var serviceClient = new IotHubServiceClient(s_connectionString);

                // Retrieve the digital twin.
                DigitalTwinGetResponse<ThermostatTwin> response = await serviceClient.DigitalTwins
                    .GetAsync<ThermostatTwin>(deviceId)
                    .ConfigureAwait(false);

                ThermostatTwin twin = response.DigitalTwin;
                twin.Metadata.ModelId.Should().Be(ThermostatModelId);
                response.ETag.Should().NotBeNull();

                // Set callback handler for receiving root-level twin property updates.
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
                {
                    Logger.Trace($"{nameof(DigitalTwinWithComponentOperationsAsync)}: DesiredProperty update received: {patch}, {context}");
                    return Task.FromResult(true);
                }, deviceClient);

                // Update the root-level property "targetTemperature".
                string propertyName = "targetTemperature";
                double propertyValue = new Random().Next(0, 100);
                var patchDocument = new JsonPatchDocument();
                patchDocument.AppendAdd($"/{propertyName}", propertyValue);
                string patch = patchDocument.ToString();
                DigitalTwinUpdateResponse updateResponse = await serviceClient.DigitalTwins.UpdateAsync(deviceId, patch);

                // Set callback to handle root-level command invocation request.
                int expectedCommandStatus = 200;
                string commandName = "getMaxMinReport";
                await deviceClient.SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithOnlyRootComponentOperationsAsync)}: Digital twin command received: {request.MethodName}.");
                        var response = new Client.DirectMethodResponse(404);

                        if (request.MethodName == commandName)
                        {
                            response.Status = expectedCommandStatus;
                            response.Payload = request.MethodName;
                        }

                        return Task.FromResult(response);
                    },
                    null);

                // Invoke the root-level command "getMaxMinReport" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                var requestOptions = new InvokeDigitalTwinCommandOptions
                {
                    Payload = JsonConvert.SerializeObject(since)
                };
                InvokeDigitalTwinCommandResponse commandResponse = await serviceClient.DigitalTwins
                    .InvokeCommandAsync(deviceId, commandName, requestOptions)
                    .ConfigureAwait(false);
                commandResponse.Status.Should().Be(expectedCommandStatus);
                commandResponse.Payload.Should().Be(JsonConvert.SerializeObject(commandName));
            }
            finally
            {
                // Delete the device.
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DigitalTwinWithComponentOperationsAsync()
        {
            // Create a new test device instance.
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string deviceId = testDevice.Id;

            try
            {
                // Create a device client instance over Mqtt, initializing it with the "TemperatureController" model which has "Thermostat" components.
                var options = new IotHubClientOptions(new IotHubClientMqttSettings())
                {
                    ModelId = TemperatureControllerModelId,
                };
                using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

                // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Perform operations on the digital twin.
                using var serviceClient = new IotHubServiceClient(s_connectionString);

                // Retrieve the digital twin.
                DigitalTwinGetResponse<TemperatureControllerTwin> response = await serviceClient.DigitalTwins
                    .GetAsync<TemperatureControllerTwin>(deviceId)
                    .ConfigureAwait(false);

                TemperatureControllerTwin twin = response.DigitalTwin;
                twin.Metadata.ModelId.Should().Be(TemperatureControllerModelId);
                response.ETag.Should().NotBeNull();

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

                var patchDocument = new JsonPatchDocument();
                patchDocument.AppendAdd($"/{componentName}", propertyValues);
                string patch = patchDocument.ToString();
                DigitalTwinUpdateResponse updateResponse = await serviceClient.DigitalTwins
                    .UpdateAsync(deviceId, patch)
                    .ConfigureAwait(false);

                // Set callbacks to handle command requests.
                int expectedCommandStatus = 200;

                // Set callback to handle both root-level and component-level command invocation requests.
                // For a component-level command, the command name is in the format "<component-name>*<command-name>".

                string rootCommandName = "reboot";
                string componentCommandName = "getMaxMinReport";
                string componentCommandNamePnp = $"{componentName}*{componentCommandName}";
                await deviceClient.SetMethodHandlerAsync(
                    (request, context) =>
                    {
                        Logger.Trace($"{nameof(DigitalTwinWithOnlyRootComponentOperationsAsync)}: Digital twin command received: {request.MethodName}.");
                        var response = new Client.DirectMethodResponse(404);

                        if (request.MethodName == rootCommandName
                            || request.MethodName == componentCommandNamePnp)
                        {
                            response.Status = expectedCommandStatus;
                            response.Payload = request.MethodName;
                        }

                        return Task.FromResult(response);
                    },
                    null);

                // Invoke the root-level command "reboot" on the digital twin.
                int delay = 1;
                var requestOptions = new InvokeDigitalTwinCommandOptions()
                {
                    Payload = JsonConvert.SerializeObject(delay)
                };
                InvokeDigitalTwinCommandResponse rootCommandResponse = await serviceClient.DigitalTwins
                    .InvokeCommandAsync(deviceId, rootCommandName, requestOptions)
                    .ConfigureAwait(false);

                rootCommandResponse.Status.Should().Be(expectedCommandStatus);
                rootCommandResponse.Payload.Should().Be(JsonConvert.SerializeObject(rootCommandName));

                // Invoke the command "getMaxMinReport" under component "thermostat1" on the digital twin.
                DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1));
                requestOptions = new InvokeDigitalTwinCommandOptions()
                {
                    Payload = JsonConvert.SerializeObject(since)
                };
                InvokeDigitalTwinCommandResponse componentCommandResponse = await serviceClient.DigitalTwins
                    .InvokeComponentCommandAsync(deviceId, componentName, componentCommandName, requestOptions)
                    .ConfigureAwait(false);

                componentCommandResponse.Status.Should().Be(expectedCommandStatus);
                componentCommandResponse.Payload.Should().Be(JsonConvert.SerializeObject(componentCommandNamePnp));
            }
            finally
            {
                // Delete the device.
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }
    }
}
