// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class TemperatureControllerSample
    {
        private const string Thermostat1Component = "thermostat1";

        private static readonly string DeviceSampleLink = 
            "https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/TemperatureController";

        private static readonly Random Random = new Random();
        private readonly DigitalTwinClient _digitalTwinClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public TemperatureControllerSample(DigitalTwinClient client, string digitalTwinId, ILogger logger)
        {
            _digitalTwinClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TemperatureControllerSample>();
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

            HttpOperationResponse<T, DigitalTwinGetHeaders> getDigitalTwinResponse = await _digitalTwinClient
                .GetDigitalTwinAsync<T>(_digitalTwinId);
            T thermostatTwin = getDigitalTwinResponse.Body;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }

        private async Task UpdateDigitalTwinComponentPropertyAsync()
        {
            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = Random.Next(0, 100);

            const string targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // First let's take a look at when the property was updated and what was it set to.
            HttpOperationResponse<TemperatureControllerTwin, DigitalTwinGetHeaders> getDigitalTwinResponse = await _digitalTwinClient
                .GetDigitalTwinAsync<TemperatureControllerTwin>(_digitalTwinId);
            ThermostatTwin thermostat1 = getDigitalTwinResponse.Body.Thermostat1;
            if (thermostat1 != null)
            {
                // Thermostat1 is present in the TemperatureController twin. We can add/replace the component-level property "targetTemperature"
                double? currentComponentTargetTemperature = getDigitalTwinResponse.Body.Thermostat1.TargetTemperature;
                if (currentComponentTargetTemperature != null)
                {
                    DateTimeOffset targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.Body.Thermostat1.Metadata.TargetTemperature.LastUpdateTime;
                    _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {Thermostat1Component} was last updated on `" +
                        $"{targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                        $" with a value of {getDigitalTwinResponse.Body.Thermostat1.Metadata.TargetTemperature.DesiredValue}.");

                    // The property path to be replaced should be prepended with a '/'
                    updateOperation.AppendReplacePropertyOp($"/{Thermostat1Component}/{targetTemperaturePropertyName}", desiredTargetTemperature);
                }
                else
                {
                    _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {Thermostat1Component} `" +
                        $"was never set on the {_digitalTwinId} digital twin.");

                    // The property path to be added should be prepended with a '/'
                    updateOperation.AppendAddPropertyOp($"/{Thermostat1Component}/{targetTemperaturePropertyName}", desiredTargetTemperature);
                }
            }
            else
            {
                // Thermostat1 is not present in the TemperatureController twin. We will add the component
                var componentProperty = new Dictionary<string, object> { { targetTemperaturePropertyName, desiredTargetTemperature }, { "$metadata", new object() } };
                _logger.LogDebug($"The component {Thermostat1Component} does not exist on the {_digitalTwinId} digital twin.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendAddComponentOp($"/{Thermostat1Component}", componentProperty);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property under component {Thermostat1Component} on the {_digitalTwinId} `" +
                $"digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient
                .UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");

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
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient
                    .InvokeCommandAsync(_digitalTwinId, rebootCommandName, JsonConvert.SerializeObject(delay));

                _logger.LogDebug($"Command {rebootCommandName} was invoked on the {_digitalTwinId} digital twin." +
                    $"\nDevice returned status: {invokeCommandResponse.Body.Status}.");
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Unable to execute command {rebootCommandName} on {_digitalTwinId}." +
                        $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
                }
            }
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            const string getMaxMinReportCommandName = "getMaxMinReport";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on component {Thermostat1Component} in the {_digitalTwinId} digital twin.");

            try
            {
                HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient
                    .InvokeComponentCommandAsync(_digitalTwinId, Thermostat1Component, getMaxMinReportCommandName, JsonConvert.SerializeObject(since));

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {Thermostat1Component}." +
                    $"\nDevice returned status: {invokeCommandResponse.Body.Status}. \nReport: {invokeCommandResponse.Body.Payload}");
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on component {Thermostat1Component}." +
                        $"\nMake sure that the device sample TemperatureController located in {DeviceSampleLink} is also running.");
                }
            }
        }
    }
}
