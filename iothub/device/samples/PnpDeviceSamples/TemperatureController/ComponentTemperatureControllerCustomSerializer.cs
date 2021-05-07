// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ComponentTemperatureControllerCustomSerializer
    {
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";

        private static readonly Random s_random = new();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public ComponentTemperatureControllerCustomSerializer(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");

            if (logger == null)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builer => builer.AddConsole());
                _logger = loggerFactory.CreateLogger<ComponentTemperatureControllerSampleNew>();
            }
            else
            {
                _logger = logger;
            }
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Send telemetry "deviceHealth" under component "thermostat1".
            var deviceHealth = new DeviceHealthNewtonSystemText
            {
                Status = "running",
                IsStopRequested = false,
            };

            using var message = new TelemetryMessage(Thermostat1)
            {
                MessageId = s_random.Next().ToString(),
                Telemetry = new TelemetryCollection
                {
                    ["deviceHealth"] = deviceHealth
                },
            };
            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerializedString()}.");

            // Retrieve the device's properties.
            ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

            // Verify if the device has previously reported a value for property
            // "initialValue" under component "thermostat2".
            // If the expected value has not been previously reported then report it.
            var initialValue = new ThermostatInitialValueSystemTextJson
            {
                Humidity = 20,
                Temperature = 25
            };

            if (!properties.TryGetValue(
                    Thermostat2,
                    "initialValue",
                    out ThermostatInitialValueSystemTextJson retrievedInitialValue)
                || !retrievedInitialValue.Equals(initialValue))
            {
                var propertiesToBeUpdated = new ClientPropertyCollection
                {
                    { "initialValue", initialValue, Thermostat2 }
                };
                ClientPropertiesUpdateResponse updateResponse = await _deviceClient
                    .UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
                _logger.LogDebug($"Property: Update - {propertiesToBeUpdated.GetSerializedString()}," +
                    $" version = {updateResponse.Version}.");
            }
            else
            {
                var tValue = properties.Get<ThermostatInitialValue>("initialValue", Thermostat2);
                _logger.LogDebug($"Property from tValue: {tValue.Humidity}.");
                _logger.LogDebug($"Property from tValue: {tValue.Temperature}.");
            }

            // Subscribe and respond to event for writable property "humidityRange"
            // under component "thermostat1".
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(
                async (writableProperties, userContext) =>
                {
                    string propertyName = "humidityRange";
                    if (!writableProperties.TryGetValue(
                            Thermostat1,
                            "humidityRange",
                            out HumidityRangeSystemTextJson humidityRangeRequested))
                    {
                        _logger.LogDebug($"Property: Update - Received a property update" +
                            $" which is not implemented.\n{writableProperties.GetSerializedString()}");
                        return;
                    }

                    _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\"," +
                        $" {{ \"{propertyName}\": {humidityRangeRequested} }}.");

                    var propertyPatch = new ClientPropertyCollection
                    {
                        { propertyName, humidityRangeRequested, StatusCodes.OK,
                            writableProperties.Version, "The operation completed successfully.", Thermostat1 }
                    };

                    ClientPropertiesUpdateResponse updateResponse = await _deviceClient
                        .UpdateClientPropertiesAsync(propertyPatch, cancellationToken);
                    _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerializedString()}\"," +
                        $" version = {updateResponse.Version} is complete.");
                },
                null,
                cancellationToken);

            // Subscribe and respond to command "updateTemperatureWithDelay" under component "thermostat2".
            await _deviceClient.SubscribeToCommandsAsync(
                async (commandRequest, userContext) =>
                {
                    try
                    {
                        UpdateTemperatureRequestSystemTextJson updateTemperatureRequest = commandRequest
                            .GetData<UpdateTemperatureRequestSystemTextJson>();

                        _logger.LogDebug($"Command: Received - component=\"{commandRequest.ComponentName}\"," +
                            $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                        await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                        var updateTemperatureResponse = new UpdateTemperatureResponseSystemTextJson
                        {
                            TargetTemperature = updateTemperatureRequest.TargetTemperature,
                            Status = StatusCodes.OK
                        };

                        _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                                    $" has {StatusCodes.OK}.");

                        return new CommandResponse(updateTemperatureResponse, StatusCodes.OK);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                        return new CommandResponse(StatusCodes.BadRequest);
                    }
                },
                null,
                cancellationToken);

            Console.ReadKey();
        }
    }
}
