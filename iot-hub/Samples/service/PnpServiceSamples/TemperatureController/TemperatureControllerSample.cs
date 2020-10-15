// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Serialization;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class TemperatureControllerSample
    {
        private const string Thermostat1Component = "thermostat1";

        private static readonly Random Random = new Random();
        private static readonly string DeviceSampleLink =
            "https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/PnpDeviceSamples/TemperatureController";

        private readonly ServiceClient _serviceClient;
        private readonly RegistryManager _registryManager;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public TemperatureControllerSample(ServiceClient serviceClient, RegistryManager registryManager, string digitalTwinId, ILogger logger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TemperatureControllerSample>();
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin
            Twin twin = await GetAndPrintDigitalTwinAsync();
            _logger.LogDebug($"Model Id of {_digitalTwinId} is: {twin.ModelId}");

            // Update the targetTemperature property on the thermostat1 component
            await UpdateDigitalTwinComponentPropertyAsync();

            // Invoke the component-level command getMaxMinReport on the thermostat1 component of the TemperatureController digital twin
            await InvokeGetMaxMinReportCommandAsync();

            // Invoke the root-level command reboot on the TemperatureController digital twin
            await InvokeRebootCommandAsync();
        }

        private async Task<Twin> GetAndPrintDigitalTwinAsync()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            Twin twin = await _registryManager.GetTwinAsync(_digitalTwinId);
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");

            return twin;
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            const string getMaxMinReportCommandName = "getMaxMinReport";

            // Create command name to invoke for component. The command is formatted as <component name>*<command name>
            string commandToInvoke = $"{Thermostat1Component}*{getMaxMinReportCommandName}";
            var commandInvocation = new CloudToDeviceMethod(commandToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set command payload
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            string componentCommandPayload = JsonConvert.SerializeObject(since);
            commandInvocation.SetPayloadJson(componentCommandPayload);

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on component {Thermostat1Component} " +
                $"in the {_digitalTwinId} digital twin.");

            try
            {
                CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(_digitalTwinId, commandInvocation);
                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {Thermostat1Component}." +
                    $"\nDevice returned status: {result.Status}. \nReport: {result.GetPayloadAsJson()}");
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on component {Thermostat1Component}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }

        private async Task InvokeRebootCommandAsync()
        {
            // Create command name to invoke for component
            const string commandToInvoke = "reboot";
            var commandInvocation = new CloudToDeviceMethod(commandToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set command payload
            string componentCommandPayload = JsonConvert.SerializeObject(3);
            commandInvocation.SetPayloadJson(componentCommandPayload);

            _logger.LogDebug($"Invoke the {commandToInvoke} command on the {_digitalTwinId} digital twin." +
                $"\nThis will set the \"targetTemperature\" on \"Thermostat\" component to 0.");

            try
            {
                CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(_digitalTwinId, commandInvocation);
                _logger.LogDebug($"Command {commandToInvoke} was invoked on the {_digitalTwinId} digital twin." +
                    $"\nDevice returned status: {result.Status}.");
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogWarning($"Unable to execute command {commandToInvoke} on component {Thermostat1Component}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }

        private async Task UpdateDigitalTwinComponentPropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";

            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = Random.Next(0, 100);

            var twinPatch = CreatePropertyPatch(targetTemperaturePropertyName, desiredTargetTemperature, Thermostat1Component);
            _logger.LogDebug($"Updating the {targetTemperaturePropertyName} property under component {Thermostat1Component} on the {_digitalTwinId} digital twin to { desiredTargetTemperature}.");

            Twin twin = await _registryManager.GetTwinAsync(_digitalTwinId);
            await _registryManager.UpdateTwinAsync(_digitalTwinId, twinPatch, twin.ETag);

            // Print the TemperatureController digital twin
            await GetAndPrintDigitalTwinAsync();
        }

        /* The property update patch (for a property within a component) needs to be in the following format:
         * {
         *  "sampleComponentName":
         *      {
         *          "__t": "c",
         *          "samplePropertyName": 20
         *      }
         *  }
         */
        private static Twin CreatePropertyPatch(string propertyName, object propertyValue, string componentName)
        {
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[componentName] = new
            {
                __t = "c"
            };
            twinPatch.Properties.Desired[componentName][propertyName] = JsonConvert.SerializeObject(propertyValue);
            return twinPatch;
        }
    }
}
