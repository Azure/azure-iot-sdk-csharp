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
        private readonly Random _random = new Random();

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
            // This sample follows the following workflow:
            // -> Set handler to receive and respond to writable property update requests.
            // -> Set handler to receive and respond to commands.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            _logger.LogDebug($"Subscribe to writable property updates.");
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(HandlePropertyUpdatesAsync, null, cancellationToken);

            _logger.LogDebug($"Subscribe to commands.");
            await _deviceClient.SubscribeToCommandsAsync(HandleCommandsAsync, null, cancellationToken);

            bool temperatureReset = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    _temperature = Math.Round(_random.NextDouble() * 40.0 + 5.0, 1);
                    temperatureReset = false;
                }

                await SendTemperatureAsync();
                await Task.Delay(5 * 1000);
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
                        const string tagetTemperatureProperty = "targetTemperature";
                        double targetTemperatureRequested = Convert.ToDouble(writableProperty.Value);
                        _logger.LogDebug($"Property: Received - {{ \"{tagetTemperatureProperty}\": {targetTemperatureRequested}°C }}.");

                        // Update Temperature in 2 steps
                        // For. eg, if the current temperature is 10 and the desired is 30, it'll go 10 (current) => 20 (in-progress) => 30 (desired).
                        double step = (targetTemperatureRequested - _temperature) / 2d;

                        _temperature = Math.Round(_temperature + step, 1);
                        var reportedPropertyInProgress = new ClientPropertyCollection();
                        reportedPropertyInProgress.Add(tagetTemperatureProperty, _temperature, StatusCodes.Accepted, writableProperties.Version);

                        ClientPropertiesUpdateResponse inProgressUpdateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedPropertyInProgress);

                        _logger.LogDebug($"Property: Update - {{ {reportedPropertyInProgress.GetSerializedString()} is {nameof(StatusCodes.Accepted)} " +
                            $"with a version of {inProgressUpdateResponse.Version}.");

                        await Task.Delay(6 * 1000);

                        _temperature = Math.Round(_temperature + step, 1);
                        var reportedProperty = new ClientPropertyCollection();
                        reportedProperty.Add(tagetTemperatureProperty, _temperature, StatusCodes.OK, writableProperties.Version, "Successfully updated target temperature");

                        ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperty);

                        _logger.LogDebug($"Property: Update - {{ {reportedProperty.GetSerializedString()} is {nameof(StatusCodes.OK)} " +
                            $"with a version of {updateResponse.Version}.");

                        break;

                    default:
                        _logger.LogWarning($"Property: Received an unrecognized property update from service:\n{{ {writableProperty.Key}: {writableProperty.Value} }}.");
                        break;
                }
            }
        }

        // The callback to handle command invocation requests.
        private Task<CommandResponse> HandleCommandsAsync(CommandRequest commandRequest, object userContext)
        {
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

        // Send temperature updates over telemetry. The sample also sends the value of max temperature since last reboot over reported property update.
        private async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();
            if (maxTemp > _maxTemp)
            {
                _maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync();
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
        private async Task UpdateMaxTemperatureSinceLastRebootAsync()
        {
            const string propertyName = "maxTempSinceLastReboot";
            var reportedProperties = new ClientPropertyCollection();
            reportedProperties.Add(propertyName, _maxTemp);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties);

            _logger.LogDebug($"Property: Update - {reportedProperties.GetSerializedString()} is {nameof(StatusCodes.OK)} " +
                $"with a version of {updateResponse.Version}.");
        }
    }
}
