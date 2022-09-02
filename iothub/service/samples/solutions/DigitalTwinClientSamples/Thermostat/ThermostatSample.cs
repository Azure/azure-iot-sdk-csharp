// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ThermostatSample
    {
        private static readonly Random s_random = new();
        private readonly DigitalTwinClient _digitalTwinClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public ThermostatSample(DigitalTwinClient client, string digitalTwinId, ILogger logger)
        {
            _digitalTwinClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin. If you do not know the exact structure of your digital twin, you can 
            // use the BasicDgitalTwin type to deserialize it to a basic type
            BasicDigitalTwin digitalTwin = await GetAndPrintDigitalTwinAsync<BasicDigitalTwin>();
            _logger.LogDebug($"The {_digitalTwinId} digital twin has a model with ID {digitalTwin.Metadata.ModelId}.");

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

            HttpOperationResponse<T, DigitalTwinGetHeaders> getDigitalTwinResponse = await _digitalTwinClient
                .GetDigitalTwinAsync<T>(_digitalTwinId);
            T thermostatTwin = getDigitalTwinResponse.Body;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }

        private async Task UpdateTargetTemperaturePropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = s_random.Next(0, 100);

            // First let's take a look at when the property was updated and what was it set to.
            HttpOperationResponse<ThermostatTwin, DigitalTwinGetHeaders> getDigitalTwinResponse = await _digitalTwinClient
                .GetDigitalTwinAsync<ThermostatTwin>(_digitalTwinId);
            double? currentTargetTemperature = getDigitalTwinResponse.Body.TargetTemperature;
            if (currentTargetTemperature != null)
            {
                DateTimeOffset targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.Body.Metadata.TargetTemperature.LastUpdateTime;
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was last updated on " +
                    $"{targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                    $" with a value of {getDigitalTwinResponse.Body.Metadata.TargetTemperature.DesiredValue}.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendReplacePropertyOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }
            else
            {
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was never set on the ${_digitalTwinId} digital twin.");

                // The property path to be added should be prepended with a '/'
                updateOperation.AppendAddPropertyOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the " +
                $"{_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient
                .UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();
        }

        private async Task UpdateCurrentTemperaturePropertyAsync()
        {
            // Choose a random value to assign to the currentTemperature property
            int currentTemperature = s_random.Next(0, 100);

            const string currentTemperaturePropertyName = "currentTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // First, add the property to the digital twin
            updateOperation.AppendAddPropertyOp($"/{currentTemperaturePropertyName}", currentTemperature);
            _logger.LogDebug($"Add the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin " +
                $"with a value of {currentTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> addPropertyToDigitalTwinResponse = await _digitalTwinClient
                .UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {addPropertyToDigitalTwinResponse.Response.StatusCode}.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();

            // Second, replace the property to a different value
            int newCurrentTemperature = s_random.Next(0, 100);

            updateOperation.AppendReplacePropertyOp($"/{currentTemperaturePropertyName}", newCurrentTemperature);
            _logger.LogDebug($"Replace the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin " +
                $"with a value of {newCurrentTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> replacePropertyInDigitalTwinResponse = await _digitalTwinClient
                .UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {replacePropertyInDigitalTwinResponse.Response.StatusCode}.");

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync<ThermostatTwin>();

            // Third, remove the currentTemperature property
            updateOperation.AppendRemoveOp($"/{currentTemperaturePropertyName}");
            _logger.LogDebug($"Remove the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> removePropertyInDigitalTwinResponse = await _digitalTwinClient
                .UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {removePropertyInDigitalTwinResponse.Response.StatusCode}.");

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
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient
                    .InvokeCommandAsync(_digitalTwinId, getMaxMinReportCommandName, JsonConvert.SerializeObject(since));

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked. \nDevice returned status: {invokeCommandResponse.Body.Status}." +
                    $"\nReport: {invokeCommandResponse.Body.Payload}");
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on {_digitalTwinId}." +
                        $"\nMake sure that the device sample Thermostat located in " +
                        $"https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/Thermostat " +
                        $"is also running.");
                }
            }
        }
    }
}
