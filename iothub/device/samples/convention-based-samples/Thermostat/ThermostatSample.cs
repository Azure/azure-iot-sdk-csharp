// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ThermostatSample
    {
        private static readonly Random s_random = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private double _temperature = 0d;
        private double _maxTemp = 0d;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<DateTimeOffset, double> _temperatureReadingsDateTimeOffset = new Dictionary<DateTimeOffset, double>();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public ThermostatSample(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException($"{nameof(deviceClient)} cannot be null.");
            _logger = logger ?? LoggerFactory.Create(builer => builer.AddConsole()).CreateLogger<ThermostatSample>();
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Set handler to receive and respond to writable property update requests.
            _logger.LogDebug($"Subscribe to writable property updates.");
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(HandlePropertyUpdatesAsync, null, cancellationToken);

            // Set handler to receive and respond to commands.
            _logger.LogDebug($"Subscribe to commands.");
            await _deviceClient.SubscribeToCommandsAsync(HandleCommandsAsync, null, cancellationToken);

            bool temperatureReset = true;

            // Periodically send "temperature" over telemetry.
            // Send "maxTempSinceLastReboot" over property update, when a new max temperature is reached.
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    _temperature = GenerateTemperatureWithinRange(45, 5);
                    temperatureReset = false;
                }

                // Send temperature updates over telemetry and the value of max temperature since last reboot over property update.
                await SendTemperatureAsync();

                await Task.Delay(s_sleepDuration);
            }
        }

        // The callback to handle property update requests.
        private async Task HandlePropertyUpdatesAsync(ClientPropertyCollection writableProperties, object userContext)
        {
            foreach (KeyValuePair<string, object> writableProperty in writableProperties)
            {
                switch (writableProperty.Key)
                {
                    case "targetTemperature":
                        const string targetTemperatureProperty = "targetTemperature";
                        double targetTemperatureRequested = Convert.ToDouble(writableProperty.Value);

                        _logger.LogDebug($"Property: Received - [ \"{targetTemperatureProperty}\": {targetTemperatureRequested}°C ].");

                        // Evaluate if this is a request that needs to be responded to, i.e.:
                        // -> The twin's last reported "targetTemperature" value is not the same as the requested value.
                        // -> The twin's last reported "targetTemperature" version is less than the requested property version.

                        // Retrieve the device's properties.
                        ClientProperties properties = await _deviceClient.GetClientPropertiesAsync();

                        bool wasTargetTemperatureReported = properties.TryGetValue(targetTemperatureProperty, out NewtonsoftJsonWritablePropertyResponse targetTemperatureWritableResponse);

                        if (wasTargetTemperatureReported)
                        {
                            double targetTemperatureReported = Convert.ToDouble(targetTemperatureWritableResponse.Value);

                            // If the last reported "targetTemperature" value is already at the requested value
                            // and with a version greater than or equal to the requested version, then the device has already responded to this request.
                            if (targetTemperatureReported == targetTemperatureRequested
                                && targetTemperatureWritableResponse.AckVersion >= writableProperties.Version)
                            {
                                _logger.LogDebug($"Property: Update - {targetTemperatureProperty} is already at {targetTemperatureRequested} " +
                                $"with a version of {targetTemperatureWritableResponse.AckVersion}. The update request with a version of {writableProperties.Version} " +
                                $"has been discarded.");
                            }
                            break;
                        }

                        _temperature = targetTemperatureRequested;
                        var reportedProperty = new ClientPropertyCollection();
                        reportedProperty.Add(targetTemperatureProperty, _temperature, StatusCodes.OK, writableProperties.Version, "Successfully updated target temperature");

                        ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperty);

                        _logger.LogDebug($"Property: Update - {reportedProperty.GetSerializedString()} is {nameof(StatusCodes.OK)} " +
                            $"with a version of {updateResponse.Version}.");

                        break;

                    default:
                        _logger.LogWarning($"Property: Received an unrecognized property update from service:\n[ {writableProperty.Key}: {writableProperty.Value} ].");
                        break;
                }
            }
        }

        // The callback to handle command invocation requests.
        private Task<CommandResponse> HandleCommandsAsync(CommandRequest commandRequest, object userContext)
        {
            // In this approach, we'll switch through the command name returned and handle each root-level command.
            switch (commandRequest.CommandName)
            {
                case "getMaxMinReport":
                    try
                    {
                        DateTimeOffset sinceInUtc = commandRequest.GetData<DateTimeOffset>();
                        _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                            $"{sinceInUtc.LocalDateTime}.");

                        Dictionary<DateTimeOffset, double> filteredReadings = _temperatureReadingsDateTimeOffset
                            .Where(i => i.Key > sinceInUtc)
                            .ToDictionary(i => i.Key, i => i.Value);

                        if (filteredReadings != null && filteredReadings.Any())
                        {
                            var report = new TemperatureReport
                            {
                                MaximumTemperature = filteredReadings.Values.Max<double>(),
                                MinimumTemperature = filteredReadings.Values.Min<double>(),
                                AverageTemperature = filteredReadings.Values.Average(),
                                StartTime = filteredReadings.Keys.Min(),
                                EndTime = filteredReadings.Keys.Max(),
                            };

                            _logger.LogDebug($"Command: MaxMinReport since {sinceInUtc.LocalDateTime}:" +
                                $" maxTemp={report.MaximumTemperature}, minTemp={report.MinimumTemperature}, avgTemp={report.AverageTemperature}, " +
                                $"startTime={report.StartTime.LocalDateTime}, endTime={report.EndTime.LocalDateTime}");

                            return Task.FromResult(new CommandResponse(report, StatusCodes.OK));
                        }

                        _logger.LogDebug($"Command: No relevant readings found since {sinceInUtc.LocalDateTime}, cannot generate any report.");

                        return Task.FromResult(new CommandResponse(StatusCodes.NotFound));
                    }
                    catch (JsonReaderException ex)
                    {
                        _logger.LogError($"Command input for {commandRequest.CommandName} is invalid: {ex.Message}.");

                        return Task.FromResult(new CommandResponse(StatusCodes.BadRequest));
                    }

                default:
                    _logger.LogWarning($"Received a command request that isn't" +
                        $" implemented - command name = {commandRequest.CommandName}");

                    return Task.FromResult(new CommandResponse(StatusCodes.NotFound));
            }
        }

        // Send temperature updates over telemetry.
        // This also sends the value of max temperature since last reboot over property update.
        private async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();
            if (maxTemp > _maxTemp)
            {
                _maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootPropertyAsync();
            }
        }

        // Send temperature update over telemetry.
        private async Task SendTemperatureTelemetryAsync()
        {
            const string telemetryName = "temperature";

            using var telemetryMessage = new TelemetryMessage
            {
                Telemetry = { [telemetryName] = _temperature }
            };
            await _deviceClient.SendTelemetryAsync(telemetryMessage);

            _logger.LogDebug($"Telemetry: Sent - {telemetryMessage.Telemetry.GetSerializedString()}.");
            _temperatureReadingsDateTimeOffset.Add(DateTimeOffset.Now, _temperature);
        }

        // Send temperature over reported property update.
        private async Task UpdateMaxTemperatureSinceLastRebootPropertyAsync()
        {
            const string propertyName = "maxTempSinceLastReboot";
            var reportedProperties = new ClientPropertyCollection();
            reportedProperties.Add(propertyName, _maxTemp);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties);

            _logger.LogDebug($"Property: Update - {reportedProperties.GetSerializedString()} is {nameof(StatusCodes.OK)} " +
                $"with a version of {updateResponse.Version}.");
        }

        private static double GenerateTemperatureWithinRange(int max = 50, int min = 0)
        {
            return Math.Round(s_random.NextDouble() * (max - min) + min, 1);
        }
    }
}
