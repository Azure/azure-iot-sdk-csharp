// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ComponentTemperatureControllerSampleNew
    {
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";

        private static readonly Random s_random = new();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public ComponentTemperatureControllerSampleNew(DeviceClient deviceClient, ILogger logger)
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
            // Retrieve the device's properties.
            ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken: cancellationToken);

            // Verify if the device has previously reported a value for property
            // "initialValue" under component "thermostat2".
            // If the expected value has not been previously reported then report it.
            var initialValue = new ThermostatInitialValue
            {
                Humidity = 20,
                Temperature = 25
            };

            if (!properties.Contains(Thermostat2)
                || !((JObject)properties[Thermostat2])
                    .TryGetValue("initialValue", out JToken initialValueReported)
                || !initialValue
                    .Equals(initialValueReported.ToObject<ThermostatInitialValue>()))
            {
                var propertiesToBeUpdated = new ClientPropertyCollection
                {
                    { "initialValue", initialValue, Thermostat2 }
                };
                await _deviceClient.UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
                _logger.LogDebug($"Property: Update - {propertiesToBeUpdated.GetSerailizedString()}.");
            }

            // Send telemetry "deviceHealth" under component "thermostat1".
            var deviceHealth = new DeviceHealth
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
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerailizedString()}.");

            // Subscribe and respond to event for writable property "humidityRange" under component "thermostat1".
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(async (writableProperties, userContext) =>
            {
                string propertyName = "humidityRange";
                if (!writableProperties.Contains(Thermostat1)
                    || !((JObject)writableProperties[Thermostat1])
                        .TryGetValue(propertyName, out JToken humidityRangeRequested))
                {
                    _logger.LogDebug($"Property: Update - Received a property update which is not implemented.\n{writableProperties.GetSerailizedString()}");
                    return;
                }

                HumidityRange targetHumidityRange = humidityRangeRequested.ToObject<HumidityRange>();
                _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\", {{ \"{propertyName}\": {targetHumidityRange} }}.");

                var propertyPatch = new ClientPropertyCollection
                {
                    { propertyName, targetHumidityRange, (int)StatusCode.Completed, writableProperties.Version, "The operation completed successfully.", Thermostat1 },
                };

                await _deviceClient.UpdateClientPropertiesAsync(propertyPatch, cancellationToken);
                _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerailizedString()}\" is complete.");
            },
            null,
            cancellationToken: cancellationToken);

            // Subscribe and respond to command "updateTemperatureWithDelay" under component "thermostat2".
            await _deviceClient.SubscribeToCommandsAsync(async (commandRequest, userContext) =>
            {
                try
                {
                    UpdateTemperatureRequest updateTemperatureRequest = commandRequest.GetData<UpdateTemperatureRequest>();

                    _logger.LogDebug($"Command: Received - component=\"{commandRequest.ComponentName}\"," +
                        $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                    await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                    var updateTemperatureResponse = new UpdateTemperatureResponse
                    {
                        TargetTemperature = updateTemperatureRequest.TargetTemperature,
                        Status = (int)StatusCode.Completed
                    };

                    _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                                $" has {StatusCode.Completed}.");

                    return new CommandResponse(updateTemperatureResponse, (int)StatusCode.Completed);
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                    return new CommandResponse((int)StatusCode.BadRequest);
                }
            },
            null,
            cancellationToken: cancellationToken);

            Console.ReadKey();
        }
    }
}
