// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ComponentTemperatureControllerSampleOld
    {
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";

        private static readonly Random s_random = new();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public ComponentTemperatureControllerSampleOld(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");

            if (logger == null)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builer => builer.AddConsole());
                _logger = loggerFactory.CreateLogger<ComponentTemperatureControllerSampleOld>();
            }
            else
            {
                _logger = logger;
            }
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Retrieve the device's properties.
            Twin properties = await _deviceClient.GetTwinAsync(cancellationToken: cancellationToken);

            // Verify if the device has previously reported a value for property "initialValue" under component "thermostat2".
            // If the expected value has not been previously reported then report it.
            var initialValue = new ThermostatInitialValue
            {
                Humidity = 20,
                Temperature = 25
            };

            if (!properties.Properties.Reported.Contains(Thermostat2)
                || !((JObject)properties.Properties.Reported[Thermostat2]).TryGetValue("initialValue", out JToken initialValueToken)
                || !initialValue.Equals(System.Text.Json.JsonSerializer.Deserialize<ThermostatInitialValue>(initialValueToken.ToString())))
            {
                var propertiesToBeUpdated = new Dictionary<string, object>
                {
                    ["__t"] = "c",
                    ["initialValue"] = initialValue
                };
                var componentProperty = new Dictionary<string, object>
                {
                    [Thermostat2] = propertiesToBeUpdated
                };
                var twinCollection = new TwinCollection(System.Text.Json.JsonSerializer.Serialize(componentProperty));
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection, cancellationToken);
                _logger.LogDebug($"Property: Update - {twinCollection.ToJson()}.");
            }

            // Send telemetry "deviceHealth" under component "thermostat1".
            var deviceHealth = new DeviceHealth
            {
                Status = "running",
                IsStopRequested = false,
            };
            var telemetry = new Dictionary<string, object>()
            {
                ["deviceHealth"] = deviceHealth
            };

            using var message = new Message(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(telemetry)))
            {
                MessageId = s_random.Next().ToString(),
                ContentEncoding = "utf-8",
                ContentType = "application/json",
                ComponentName = Thermostat1
            };

            await _deviceClient.SendEventAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {System.Text.Json.JsonSerializer.Serialize(telemetry)}.");

            // Subscribe and respond to event for writable property "humidityRange" under component "thermostat1".
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desired, userContext) =>
            {
                string propertyName = "humidityRange";
                if (!desired.Contains(Thermostat1) || !((JObject)desired[Thermostat1]).TryGetValue(propertyName, out JToken humidityRangeToken))
                {
                    _logger.LogDebug($"Property: Update - Received a property update which is not implemented.\n{desired.ToJson()}");
                    return;
                }

                HumidityRange targetHumidityRange = System.Text.Json.JsonSerializer.Deserialize<HumidityRange>(humidityRangeToken.ToString());

                var propertyPatch = new Dictionary<string, object>();
                var componentPatch = new Dictionary<string, object>
                {
                    ["__t"] = "c"
                };
                var temperatureUpdateResponse = new
                {
                    value = targetHumidityRange,
                    ac = (int)StatusCode.Completed,
                    av = desired.Version,
                    ad = "The operation completed successfully."
                };
                componentPatch[propertyName] = temperatureUpdateResponse;
                propertyPatch[Thermostat1] = componentPatch;

                _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\", {{ \"{propertyName}\": {targetHumidityRange} }}.");

                var twinCollection = new TwinCollection(System.Text.Json.JsonSerializer.Serialize(propertyPatch));
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection, cancellationToken);
                _logger.LogDebug($"Property: Update - \"{twinCollection.ToJson()}\" is complete.");
            },
            null,
            cancellationToken: cancellationToken);

            // Subscribe and respond to command "updateTemperatureWithDelay" under component "thermostat2".
            await _deviceClient.SetMethodHandlerAsync($"{Thermostat2}*updateTemperatureWithDelay", async (commandRequest, userContext) =>
            {
                try
                {
                    UpdateTemperatureRequest updateTemperatureRequest = System.Text.Json.JsonSerializer.Deserialize<UpdateTemperatureRequest>(commandRequest.DataAsJson);

                    _logger.LogDebug($"Command: Received - component=\"{Thermostat2}\"," +
                        $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                    await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                    var updateTemperatureResponse = new UpdateTemperatureResponse
                    {
                        TargetTemperature = updateTemperatureRequest.TargetTemperature,
                        Status = (int)StatusCode.Completed
                    };

                    _logger.LogDebug($"Command: component=\"{Thermostat2}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                                $" has {StatusCode.Completed}.");

                    return new MethodResponse(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(updateTemperatureResponse)), (int)StatusCode.Completed);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                    return new MethodResponse((int)StatusCode.BadRequest);
                }
            },
            null,
            cancellationToken);

            Console.ReadKey();
        }
    }
}
