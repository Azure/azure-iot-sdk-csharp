// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            // Send telemetry "deviceHealth" under component "thermostat1".
            var deviceHealth = new DeviceHealthNewtonSoftJson
            {
                Status = "running",
                IsStopRequested = false,
            };
            var telemetry = new Dictionary<string, object>()
            {
                ["deviceHealth"] = deviceHealth
            };

            using var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry)))
            {
                MessageId = s_random.Next().ToString(),
                ContentEncoding = "utf-8",
                ContentType = "application/json",
                ComponentName = Thermostat1
            };
            await _deviceClient.SendEventAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {JsonConvert.SerializeObject(telemetry)}.");

            // Retrieve the device's properties.
            Twin properties = await _deviceClient.GetTwinAsync(cancellationToken: cancellationToken);

            // Verify if the device has previously reported a value for property
            // "initialValue" under component "thermostat2".
            // If the expected value has not been previously reported then report it.
            var initialValue = new ThermostatInitialValueNewtonSoftJson
            {
                Humidity = 20,
                Temperature = 25
            };

            if (!properties.Properties.Reported.Contains(Thermostat2)
                || !((JObject)properties.Properties.Reported[Thermostat2])
                    .TryGetValue("initialValue", out JToken retrievedInitialValue)
                || !initialValue
                    .Equals(retrievedInitialValue.ToObject<ThermostatInitialValueNewtonSoftJson>()))
            {
                var propertiesToBeUpdated = new TwinCollection
                {
                    ["__t"] = "c",
                    ["initialValue"] = initialValue
                };
                var componentProperty = new TwinCollection
                {
                    [Thermostat2] = propertiesToBeUpdated
                };
                await _deviceClient.UpdateReportedPropertiesAsync(componentProperty, cancellationToken);
                _logger.LogDebug($"Property: Update - {componentProperty.ToJson()}.");
            }

            // Subscribe and respond to event for writable property "humidityRange"
            // under component "thermostat1".
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                async (desired, userContext) =>
                {
                    string propertyName = "humidityRange";
                    if (!desired.Contains(Thermostat1)
                        || !((JObject)desired[Thermostat1])
                            .TryGetValue(propertyName, out JToken humidityRangeRequested))
                    {
                        _logger.LogDebug($"Property: Update - Received a property update" +
                            $" which is not implemented.\n{desired.ToJson()}");
                        return;
                    }

                    HumidityRangeNewtonSoftJson targetHumidityRange = humidityRangeRequested
                        .ToObject<HumidityRangeNewtonSoftJson>();
                    _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\"," +
                        $" {{ \"{propertyName}\": {targetHumidityRange} }}.");

                    var propertyPatch = new TwinCollection();
                    var componentPatch = new TwinCollection()
                    {
                        ["__t"] = "c"
                    };
                    var humidityUpdateResponse = new TwinCollection
                    {
                        ["value"] = targetHumidityRange,
                        ["ac"] = StatusCodes.OK,
                        ["av"] = desired.Version,
                        ["ad"] = "The operation completed successfully."
                    };
                    componentPatch[propertyName] = humidityUpdateResponse;
                    propertyPatch[Thermostat1] = componentPatch;

                    _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\", {{ \"{propertyName}\": {targetHumidityRange} }}.");

                    await _deviceClient.UpdateReportedPropertiesAsync(propertyPatch, cancellationToken);
                    _logger.LogDebug($"Property: Update - \"{propertyPatch.ToJson()}\" is complete.");
                },
                null,
                cancellationToken);

            // Subscribe and respond to command "updateTemperatureWithDelay" under component "thermostat2".
            await _deviceClient.SetMethodHandlerAsync(
                $"{Thermostat2}*updateTemperatureWithDelay",
                async (commandRequest, userContext) =>
                {
                    try
                    {
                        UpdateTemperatureRequestNewtonSoftJson updateTemperatureRequest = JsonConvert
                            .DeserializeObject<UpdateTemperatureRequestNewtonSoftJson>(commandRequest.DataAsJson);

                        _logger.LogDebug($"Command: Received - component=\"{Thermostat2}\"," +
                            $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                        await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                        var updateTemperatureResponse = new UpdateTemperatureResponseNewtonSoftJson
                        {
                            TargetTemperature = updateTemperatureRequest.TargetTemperature,
                            Status = StatusCodes.OK
                        };

                        _logger.LogDebug($"Command: component=\"{Thermostat2}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                                    $" has {StatusCodes.OK}.");

                        return new MethodResponse(
                            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updateTemperatureResponse)),
                            StatusCodes.OK);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                        return new MethodResponse(StatusCodes.BadRequest);
                    }
                },
                null,
                cancellationToken);

            Console.ReadKey();
        }
    }
}
