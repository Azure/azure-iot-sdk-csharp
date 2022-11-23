// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    public class ThermostatSample
    {
        private static readonly Random s_random = new();
        private readonly IotHubServiceClient _serviceClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public ThermostatSample(IotHubServiceClient client, string digitalTwinId, ILogger logger)
        {
            _serviceClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin. If you do not know the exact structure of your digital twin, you can 
            // use the BasicDgitalTwin type to deserialize it to a basic type
            BasicDigitalTwin digitalTwin = await GetAndPrintDigitalTwinAsync<BasicDigitalTwin>();
            _logger.LogDebug($"The {_digitalTwinId} digital twin has a model with Id {digitalTwin.Metadata.ModelId}.");

            // Update the targetTemperature property of the digital twin
            await UpdateTargetTemperaturePropertyAsync();

            // Add, replace then remove currentTemperature property on the digital twin. Note that the 
            // currentTemperature property does not exist on the model that the device is registered with
            await UpdateCurrentTemperaturePropertyAsync();

            // Invoke the root-level command getMaxMinReport command on the digital twin
            await InvokeGetMaxMinReportCommandAsync();
        }

        private async Task<T> GetAndPrintDigitalTwinAsync<T>()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            DigitalTwinGetResponse<T> getDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .GetAsync<T>(_digitalTwinId);
            T thermostatTwin = getDigitalTwinResponse.DigitalTwin;

            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonSerializer.Serialize(thermostatTwin, new JsonSerializerOptions { WriteIndented = true })}");

            return thermostatTwin;
        }

        private async Task UpdateTargetTemperaturePropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new JsonPatchDocument();

            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = s_random.Next(0, 100);

            // First let's take a look at when the property was updated and what was it set to.
            DigitalTwinGetResponse<ThermostatTwin> getDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .GetAsync<ThermostatTwin>(_digitalTwinId);
            double? currentTargetTemperature = getDigitalTwinResponse.DigitalTwin.TargetTemperature;
            if (currentTargetTemperature != null)
            {
                DateTimeOffset targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.DigitalTwin.Metadata.TargetTemperature.LastUpdatedOnUtc;
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was last updated on " +
                    $"{targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                    $" with a value of {getDigitalTwinResponse.DigitalTwin.Metadata.TargetTemperature.DesiredValue}.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendReplace($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }
            else
            {
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was never set on the ${_digitalTwinId} digital twin.");

                // The property path to be added should be prepended with a '/'
                updateOperation.AppendAdd($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the " +
                $"{_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            DigitalTwinUpdateResponse updateDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .UpdateAsync(_digitalTwinId, updateOperation.ToString());

            _logger.LogDebug($"Updated {_digitalTwinId} digital twin.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();
        }

        private async Task UpdateCurrentTemperaturePropertyAsync()
        {
            // Choose a random value to assign to the currentTemperature property
            int currentTemperature = s_random.Next(0, 100);

            const string currentTemperaturePropertyName = "currentTemperature";
            var updateOperation = new JsonPatchDocument();

            // First, add the property to the digital twin
            updateOperation.AppendAdd($"/{currentTemperaturePropertyName}", currentTemperature);
            _logger.LogDebug($"Add the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin " +
                $"with a value of {currentTemperature}.");
            DigitalTwinUpdateResponse addPropertyToDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .UpdateAsync(_digitalTwinId, updateOperation.ToString());
            _logger.LogDebug($"Updated {_digitalTwinId} digital twin");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();

            // Second, replace the property to a different value
            int newCurrentTemperature = s_random.Next(0, 100);

            updateOperation.AppendReplace($"/{currentTemperaturePropertyName}", newCurrentTemperature);
            _logger.LogDebug($"Replace the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin " +
                $"with a value of {newCurrentTemperature}.");
            DigitalTwinUpdateResponse replacePropertyInDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .UpdateAsync(_digitalTwinId, updateOperation.ToString());
            _logger.LogDebug($"Updated {_digitalTwinId} digital twin.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();

            // Third, remove the currentTemperature property
            updateOperation.AppendRemove($"/{currentTemperaturePropertyName}");
            _logger.LogDebug($"Remove the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin.");
            DigitalTwinUpdateResponse removePropertyInDigitalTwinResponse = await _serviceClient
                .DigitalTwins
                .UpdateAsync(_digitalTwinId, updateOperation.ToString());
            _logger.LogDebug($"Updated {_digitalTwinId} digital twin.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            const string getMaxMinReportCommandName = "getMaxMinReport";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on {_digitalTwinId} digital twin.");

            try
            {
                InvokeDigitalTwinCommandResponse invokeCommandResponse = await _serviceClient
                    .DigitalTwins
                    .InvokeCommandAsync(
                        _digitalTwinId,
                        getMaxMinReportCommandName,
                        new InvokeDigitalTwinCommandOptions { Payload = JsonSerializer.Serialize(since) });

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked. \nDevice returned status: {invokeCommandResponse.Status}." +
                    $"\nReport: {invokeCommandResponse.Payload}");
            }
            catch (IotHubServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on {_digitalTwinId}." +
                    $"\nMake sure that the device sample Thermostat located in " +
                    $"https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/Thermostat " +
                    $"is also running.");
            }
        }
    }
}
