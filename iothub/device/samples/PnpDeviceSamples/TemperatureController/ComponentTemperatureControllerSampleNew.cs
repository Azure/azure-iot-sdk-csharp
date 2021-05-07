// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Example status code extension class
    /// </summary>
    public class StatusCodesCustom : StatusCodes
    {
        /// <summary>
        /// Using a non-standard 3 digit code. Can use anything from -int32 to +int32
        /// </summary>
        public static int MyExtendedCode => 909;
    }
}

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
            // Send telemetry "deviceHealth" under component "thermostat1".
            var deviceHealth = new DeviceHealthNewtonSoftJson
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
            var initialValue = new ThermostatInitialValueNewtonSoftJson
            {
                Humidity = 20,
                Temperature = 25
            };

            if (!properties.TryGetValue(
                    Thermostat2,
                    "initialValue",
                    out ThermostatInitialValueNewtonSoftJson retrievedInitialValue)
                || !retrievedInitialValue.Equals(initialValue))
            {
                var propertiesToBeUpdated = new ClientPropertyCollection();
                propertiesToBeUpdated.Add("initialValue", initialValue, Thermostat2);

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
                            out HumidityRangeNewtonSoftJson humidityRangeRequested))
                    {
                        _logger.LogDebug($"Property: Update - Received a property update" +
                            $" which is not implemented.\n{writableProperties.GetSerializedString()}");
                        return;
                    }

                    _logger.LogDebug($"Property: Received - component=\"{Thermostat1}\"," +
                        $" {{ \"{propertyName}\": {humidityRangeRequested} }}.");

                    var propertyPatch = new ClientPropertyCollection();
                    propertyPatch.Add(
                        propertyName,
                        humidityRangeRequested,
                        StatusCodes.OK,
                        writableProperties.Version,
                        "The operation completed successfully.",
                        Thermostat1);

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
                        switch (commandRequest.ComponentName)
                        {
                            case Thermostat2:
                                switch (commandRequest.CommandName)
                                {
                                    case "updateTemperatureWithDelay":
                                        UpdateTemperatureRequestNewtonSoftJson updateTemperatureRequest = commandRequest
                                            .GetData<UpdateTemperatureRequestNewtonSoftJson>();

                                        _logger.LogDebug($"Command: Received - component=\"{commandRequest.ComponentName}\"," +
                                            $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                                        await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                                        var updateTemperatureResponse = new UpdateTemperatureResponseNewtonSoftJson
                                        {
                                            TargetTemperature = updateTemperatureRequest.TargetTemperature,
                                            Status = StatusCodes.OK
                                        };

                                        _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                                                    $" has {StatusCodes.OK}.");

                                        return new CommandResponse(updateTemperatureResponse, StatusCodes.OK);

                                    default:
                                        _logger.LogWarning($"Received a command request that isn't implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");
                                        return new CommandResponse(StatusCodes.NotFound);
                                }

                            default:
                                _logger.LogWarning($"Received a command request that isn't implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");
                                return new CommandResponse(StatusCodes.NotFound);
                        }
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
