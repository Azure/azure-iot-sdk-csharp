// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ThermostatSample
    {
        private static readonly Random Random = new Random();
        private readonly ServiceClient _serviceClient;
        private readonly RegistryManager _registryManager;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public ThermostatSample(ServiceClient serviceClient, RegistryManager registryManager, string digitalTwinId, ILogger logger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ThermostatSample>();
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin
            Twin digitalTwin = await GetAndPrintDigitalTwinAsync();
            _logger.LogDebug($"The {_digitalTwinId} digital twin has a model with ID {digitalTwin.ModelId}.");

            // Update the targetTemperature property of the digital twin
            await UpdateTargetTemperaturePropertyAsync();

            // Invoke the root-level command getMaxMinReport command on the digital twin
            await InvokeGetMaxMinReportCommandAsync();
        }

        private async Task<Twin> GetAndPrintDigitalTwinAsync()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            Twin twin = await _registryManager.GetTwinAsync(_digitalTwinId);
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");

            return twin;
        }

        private async Task UpdateTargetTemperaturePropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";
            Twin twin = await _registryManager.GetTwinAsync(_digitalTwinId);

            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = Random.Next(0, 100);

            // Update the twin
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[targetTemperaturePropertyName] = desiredTargetTemperature;

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the " +
                $"{_digitalTwinId} digital twin to {desiredTargetTemperature}.");

            await _registryManager.UpdateTwinAsync(_digitalTwinId, twinPatch, twin.ETag);

            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwinAsync();
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            const string getMaxMinReportCommandName = "getMaxMinReport";

            // Create command name to invoke for component
            var commandInvocation = new CloudToDeviceMethod(getMaxMinReportCommandName) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set command payload
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            string componentCommandPayload = JsonConvert.SerializeObject(since);
            commandInvocation.SetPayloadJson(componentCommandPayload);

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on {_digitalTwinId} digital twin.");
            try
            {
                CloudToDeviceMethodResult result = await _serviceClient.InvokeDeviceMethodAsync(_digitalTwinId, commandInvocation);

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on digital twin {_digitalTwinId}." +
                    $"\nDevice returned status: {result.Status}. \nReport: {result.GetPayloadAsJson()}");
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on {_digitalTwinId}." +
                    $"\nMake sure that the device sample Thermostat located in " +
                    $"https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/PnpDeviceSamples/Thermostat " +
                    $"is also running.");
            }
        }
    }
}
