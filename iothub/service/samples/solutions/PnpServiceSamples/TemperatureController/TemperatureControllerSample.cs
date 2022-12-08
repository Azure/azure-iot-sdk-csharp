// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    public class TemperatureControllerSample
    {
        private const string Thermostat1Component = "thermostat1";

        private static readonly Random s_random = new();
        private const string DeviceSampleLink =
            "https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/TemperatureController";

        private readonly IotHubServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public TemperatureControllerSample(IotHubServiceClient serviceClient, string deviceId, ILogger logger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunSampleAsync()
        {
            // Get and print the device twin
            ClientTwin twin = await GetAndPrintDeviceTwinAsync();
            _logger.LogDebug($"Model Id of {_deviceId} is: {twin.ModelId}");

            // Update the targetTemperature property on the thermostat1 component
            await UpdateDeviceTwinComponentPropertyAsync();

            // Invoke the component-level command getMaxMinReport on the thermostat1 component of the TemperatureController device twin
            await InvokeGetMaxMinReportCommandAsync();

            // Invoke the root-level command reboot on the TemperatureController device twin
            await InvokeRebootCommandAsync();
        }

        private async Task<ClientTwin> GetAndPrintDeviceTwinAsync()
        {
            _logger.LogDebug($"Get the {_deviceId} device twin.");

            ClientTwin twin = await _serviceClient.Twins.GetAsync(_deviceId);
            _logger.LogDebug($"{_deviceId} twin: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");

            return twin;
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            const string getMaxMinReportCommandName = "getMaxMinReport";

            // Create command name to invoke for component. The command is formatted as <component name>*<command name>
            string commandName = $"{Thermostat1Component}*{getMaxMinReportCommandName}";
            var commandInvocation = new DirectMethodServiceRequest(commandName)
            { 
                ResponseTimeout = TimeSpan.FromSeconds(30),
                Payload = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2)),
            };

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on component {Thermostat1Component} " +
                $"in the {_deviceId} device twin.");

            try
            {
                DirectMethodClientResponse result = await _serviceClient.DirectMethods.InvokeAsync(_deviceId, commandInvocation);
                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {Thermostat1Component}." +
                    $"\nDevice returned status: {result.Status}. \nReport: {result.PayloadAsString}");
            }
            catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotFound)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on component {Thermostat1Component}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }

        private async Task InvokeRebootCommandAsync()
        {
            // Create command name to invoke for component
            const string commandName = "reboot";
            var commandInvocation = new DirectMethodServiceRequest(commandName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
                Payload = JsonConvert.SerializeObject(3),
            };

            _logger.LogDebug($"Invoke the {commandName} command on the {_deviceId} device twin." +
                $"\nThis will set the \"targetTemperature\" on \"Thermostat\" component to 0.");

            try
            {
                DirectMethodClientResponse result = await _serviceClient.DirectMethods.InvokeAsync(_deviceId, commandInvocation);
                _logger.LogDebug($"Command {commandName} was invoked on the {_deviceId} device twin." +
                    $"\nDevice returned status: {result.Status}.");
            }
            catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotFound)
            {
                _logger.LogWarning($"Unable to execute command {commandName} on component {Thermostat1Component}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }

        private async Task UpdateDeviceTwinComponentPropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";

            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = s_random.Next(0, 100);

            var twinPatch = CreatePropertyPatch(targetTemperaturePropertyName, desiredTargetTemperature, Thermostat1Component);
            _logger.LogDebug($"Updating the {targetTemperaturePropertyName} property under component {Thermostat1Component} on the {_deviceId} device twin to { desiredTargetTemperature}.");

            ClientTwin currentTwin = await _serviceClient.Twins.GetAsync(_deviceId);
            twinPatch.ETag = currentTwin.ETag;
            await _serviceClient.Twins.UpdateAsync(_deviceId, twinPatch, true);

            // Print the TemperatureController device twin
            await GetAndPrintDeviceTwinAsync();
        }

        // The property update patch (for a property within a component) needs to be in the following format:
        // {
        //   "sampleComponentName":
        //     {
        //       "__t": "c",
        //         "samplePropertyName": 20
        //     }
        // }
        private static ClientTwin CreatePropertyPatch(string propertyName, object propertyValue, string componentName)
        {
            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[componentName] = new { __t = "c" };
            twinPatch.Properties.Desired[componentName][propertyName] = JsonConvert.SerializeObject(propertyValue);
            return twinPatch;
        }
    }
}
