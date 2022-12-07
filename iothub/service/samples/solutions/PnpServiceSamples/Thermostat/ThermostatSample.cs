// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    public class ThermostatSample
    {
        private static readonly Random s_random = new();
        private readonly IotHubServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public ThermostatSample(IotHubServiceClient serviceClient, string deviceId, ILogger logger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunSampleAsync()
        {
            // Get and print the device twin
            ClientTwin deviceTwin = await GetAndPrintDeviceTwinAsync();
            _logger.LogDebug($"The {_deviceId} device twin has a model with ID {deviceTwin.ModelId}.");

            // Update the targetTemperature property of the device twin
            await UpdateTargetTemperaturePropertyAsync();

            // Invoke the root-level command getMaxMinReport command on the device twin
            await InvokeGetMaxMinReportCommandAsync();
        }

        private async Task<ClientTwin> GetAndPrintDeviceTwinAsync()
        {
            _logger.LogDebug($"Get the {_deviceId} device twin.");

            ClientTwin twin = await _serviceClient.Twins.GetAsync(_deviceId);
            _logger.LogDebug($"{_deviceId} twin: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");

            return twin;
        }

        private async Task UpdateTargetTemperaturePropertyAsync()
        {
            const string targetTemperaturePropertyName = "targetTemperature";
            ClientTwin twin = await _serviceClient.Twins.GetAsync(_deviceId);

            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = s_random.Next(0, 100);

            // Update the twin
            var twinPatch = new ClientTwin
            {
                ETag = twin.ETag,
            };
            twinPatch.Properties.Desired[targetTemperaturePropertyName] = desiredTargetTemperature;

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the " +
                $"{_deviceId} device twin to {desiredTargetTemperature}.");

            await _serviceClient.Twins.UpdateAsync(_deviceId, twinPatch);

            // Print the Thermostat device twin
            await GetAndPrintDeviceTwinAsync();
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
        {
            const string getMaxMinReportCommandName = "getMaxMinReport";

            // Create command name to invoke for component
            var commandInvocation = new DirectMethodServiceRequest(getMaxMinReportCommandName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
                Payload = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2)),
            };

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on {_deviceId} device twin.");
            try
            {
                DirectMethodClientResponse result = await _serviceClient.DirectMethods.InvokeAsync(_deviceId, commandInvocation);

                _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on device twin {_deviceId}." +
                    $"\nDevice returned status: {result.Status}. \nReport: {result.PayloadAsString}");
            }
            catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotFound)
            {
                _logger.LogWarning($"Unable to execute command {getMaxMinReportCommandName} on {_deviceId}." +
                    $"\nMake sure that the device sample Thermostat located in " +
                    $"https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/PnpDeviceSamples/Thermostat " +
                    $"is also running.");
            }
        }
    }
}
