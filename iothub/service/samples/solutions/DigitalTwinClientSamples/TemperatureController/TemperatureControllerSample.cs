// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    public class TemperatureControllerSample
    {
        private const string Thermostat1Component = "thermostat1";

        private const string DeviceSampleLink =
            "https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/TemperatureController";

        private static readonly Random s_random = new();
        private readonly IotHubServiceClient _hubClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public TemperatureControllerSample(IotHubServiceClient client, string digitalTwinId, ILogger logger)
        {
            _hubClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin
            TemperatureControllerTwin digitalTwin = await GetAndPrintDigitalTwinAsync<TemperatureControllerTwin>();
            _logger.LogDebug($"The {_digitalTwinId} digital twin has a model with ID {digitalTwin.Metadata.ModelId}.");

            // Update the targetTemperature property on the thermostat1 component
            await UpdateDigitalTwinComponentPropertyAsync();

            // Invoke the component-level command getMaxMinReport on the thermostat1 component of the TemperatureController digital twin
            await InvokeGetMaxMinReportCommandAsync();

            // Invoke the root-level command reboot on the TemperatureController digital twin
            await InvokeRebootCommandAsync();
        }

        private async Task<T> GetAndPrintDigitalTwinAsync<T>()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            DigitalTwinGetResponse<T> getDigitalTwinResponse = await _hubClient
                .DigitalTwins
                .GetAsync<T>(_digitalTwinId);
            T thermostatTwin = getDigitalTwinResponse.DigitalTwin;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }

        private async Task UpdateDigitalTwinComponentPropertyAsync()
        {
            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = s_random.Next(0, 100);

            const string targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new JsonPatchDocument();

            // First let's take a look at when the property was updated and what was it set to.
            DigitalTwinGetResponse<TemperatureControllerTwin> getDigitalTwinResponse = await _hubClient
                .DigitalTwins
                .GetAsync<TemperatureControllerTwin>(_digitalTwinId);
            ThermostatTwin thermostat1 = getDigitalTwinResponse.DigitalTwin.Thermostat1;
            if (thermostat1 != null)
            {
                // Thermostat1 is present in the TemperatureController twin. We can add/replace the component-level property "targetTemperature"
                double? currentComponentTargetTemperature = getDigitalTwinResponse.DigitalTwin.Thermostat1.TargetTemperature;
                if (currentComponentTargetTemperature != null)
                {
                    DateTimeOffset targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.DigitalTwin.Thermostat1.Metadata.TargetTemperature.LastUpdatedOn;
                    _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {Thermostat1Component} was last updated on `" +
                        $"{targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                        $" with a value of {getDigitalTwinResponse.DigitalTwin.Thermostat1.Metadata.TargetTemperature.DesiredValue}.");

                    // The property path to be replaced should be prepended with a '/'
                    updateOperation.AppendReplace($"/{Thermostat1Component}/{targetTemperaturePropertyName}", desiredTargetTemperature);
                }
                else
                {
                    _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {Thermostat1Component} `" +
                        $"was never set on the {_digitalTwinId} digital twin.");

                    // The property path to be added should be prepended with a '/'
                    updateOperation.AppendReplace($"/{Thermostat1Component}/{targetTemperaturePropertyName}", desiredTargetTemperature);
                }
            }
            else
            {
                // Thermostat1 is not present in the TemperatureController twin. We will add the component
                var componentProperty = new Dictionary<string, object> { { targetTemperaturePropertyName, desiredTargetTemperature }, { "$metadata", new object() } };
                _logger.LogDebug($"The component {Thermostat1Component} does not exist on the {_digitalTwinId} digital twin.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendReplace($"/{Thermostat1Component}", componentProperty);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property under component {Thermostat1Component} on the {_digitalTwinId} `" +
                $"digital twin to {desiredTargetTemperature}.");
            DigitalTwinUpdateResponse updateDigitalTwinResponse = await _hubClient
                .DigitalTwins
                .UpdateAsync(_digitalTwinId, updateOperation.ToString());

            _logger.LogDebug($"Updated digital twin {_digitalTwinId}.");

            // Print the TemperatureController digital twin
            await GetAndPrintDigitalTwinAsync<TemperatureControllerTwin>();
        }

        private async Task InvokeRebootCommandAsync()
        {
            const int delay = 1;
            const string rebootCommandName = "reboot";

            _logger.LogDebug($"Invoke the {rebootCommandName} command on the {_digitalTwinId} digital twin." +
                $"\nThis will set the \"targetTemperature\" on \"Thermostat\" component to 0.");

            try
            {
                var options = new InvokeDigitalTwinCommandOptions
                {
                    Payload = JsonConvert.SerializeObject(delay),
                };
                InvokeDigitalTwinCommandResponse invokeCommandResponse = await _hubClient
                    .DigitalTwins
                    .InvokeCommandAsync(_digitalTwinId, rebootCommandName, options);

                _logger.LogDebug($"Command {rebootCommandName} was invoked on the {_digitalTwinId} digital twin." +
                    $"\nDevice returned status: {invokeCommandResponse.Status}.");
            }
            catch (IotHubServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Unable to execute command {rebootCommandName} on {_digitalTwinId}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            const string getMaxMinReportCommandName = "getMaxMinReport";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on component {Thermostat1Component} in the {_digitalTwinId} digital twin.");

            try
            {
                var options = new InvokeDigitalTwinCommandOptions
                {
                    Payload = JsonConvert.SerializeObject(since),
                };
                InvokeDigitalTwinCommandResponse invokeCommandResponse = await _hubClient
                    .DigitalTwins
                    .InvokeComponentCommandAsync(_digitalTwinId, Thermostat1Component, getMaxMinReportCommandName, options);

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {Thermostat1Component}." +
                    $"\nDevice returned status: {invokeCommandResponse.Status}. \nReport: {invokeCommandResponse.Payload}");
            }
            catch (IotHubServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on component {Thermostat1Component}." +
                    $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
            }
        }
    }
}
