// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class TemperatureControllerSample
    {
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";

        private static readonly Random s_random = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this
        // information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<string, Dictionary<DateTimeOffset, double>> _temperatureReadingsDateTimeOffset =
            new Dictionary<string, Dictionary<DateTimeOffset, double>>();

        // Dictionary to hold the current temperature for each "Thermostat" component.
        private readonly Dictionary<string, double> _temperature = new Dictionary<string, double>();

        // Dictionary to hold the max temperature since last reboot, for each "Thermostat" component.
        private readonly Dictionary<string, double> _maxTemp = new Dictionary<string, double>();

        public TemperatureControllerSample(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException($"{nameof(deviceClient)} cannot be null.");
            _logger = logger ?? LoggerFactory.Create(builer => builer.AddConsole()).CreateLogger<TemperatureControllerSample>();
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Set handler to receive and respond to writable property update requests.
            _logger.LogDebug("Subscribe to writable property updates.");
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(HandlePropertyUpdatesAsync, null, cancellationToken);

            // Set handler to receive and respond to commands.
            _logger.LogDebug($"Subscribe to commands.");
            await _deviceClient.SubscribeToCommandsAsync(HandleCommandsAsync, null, cancellationToken);

            // Report device information on "deviceInformation" component.
            // This is a component-level property update call.
            await UpdateDeviceInformationPropertyAsync(cancellationToken);

            // Verify if the device has previously reported the current value for property "serialNumber".
            // If the expected value has not been previously reported then send device serial number over property update.
            // This is a top-level property update call.
            await SendDeviceSerialNumberPropertyIfNotCurrentAsync(cancellationToken);

            bool temperatureReset = true;
            _maxTemp[Thermostat1] = 0d;
            _maxTemp[Thermostat2] = 0d;

            // Periodically send "temperature" over telemetry - on "Thermostat" components.
            // Send "maxTempSinceLastReboot" over property update, when a new max temperature is reached - on "Thermostat" components.
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading for each "Thermostat" component.
                    _temperature[Thermostat1] = GenerateTemperatureWithinRange(45, 5);
                    _temperature[Thermostat2] = GenerateTemperatureWithinRange(45, 5);
                }

                // Send temperature updates over telemetry and the value of max temperature since last reboot over property update.
                // Both of these are component-level calls.
                await SendTemperatureAsync(Thermostat1, cancellationToken);
                await SendTemperatureAsync(Thermostat2, cancellationToken);

                // Send working set of device memory over telemetry.
                // This is a top-level telemetry call.
                await SendDeviceMemoryTelemetryAsync(cancellationToken);

                temperatureReset = _temperature[Thermostat1] == 0 && _temperature[Thermostat2] == 0;
                await Task.Delay(s_sleepDuration);
            }
        }

        // The callback to handle property update requests.
        private async Task HandlePropertyUpdatesAsync(ClientPropertyCollection writableProperties, object userContext)
        {
            foreach (KeyValuePair<string, object> writableProperty in writableProperties)
            {
                // The dispatcher key will be either a top-level property name or a component name.
                switch (writableProperty.Key)
                {
                    case Thermostat1:
                    case Thermostat2:
                        const string targetTemperatureProperty = "targetTemperature";
                        if (writableProperties.TryGetValue(writableProperty.Key, targetTemperatureProperty, out double targetTemperatureRequested))
                        {
                            await HandleTargetTemperatureUpdateRequestAsync(writableProperty.Key, targetTemperatureRequested, writableProperties.Version, userContext);
                            break;
                        }
                        else
                        {
                            _logger.LogWarning($"Property: Received an unrecognized property update from service for component {writableProperty.Key}:" +
                                        $"\n[ {writableProperty.Value} ].");
                            break;
                        }

                    default:
                        _logger.LogWarning($"Property: Received an unrecognized property update from service:" +
                                        $"\n[ {writableProperty.Key}: {writableProperty.Value} ].");
                        break;
                }
            }
        }

        // The callback to handle target temperature property update requests for a component.
        private async Task HandleTargetTemperatureUpdateRequestAsync(string componentName, double targetTemperature, long version, object userContext)
        {
            const string targetTemperatureProperty = "targetTemperature";
            _logger.LogDebug($"Property: Received - component=\"{componentName}\", [ \"{targetTemperatureProperty}\": {targetTemperature}°C ].");

            _temperature[componentName] = targetTemperature;
            IWritablePropertyResponse writableResponse = _deviceClient
                .PayloadConvention
                .PayloadSerializer
                .CreateWritablePropertyResponse(_temperature[componentName], CommonClientResponseCodes.OK, version, "Successfully updated target temperature.");

            var reportedProperty = new ClientPropertyCollection();
            reportedProperty.AddComponentProperty(componentName, targetTemperatureProperty, writableResponse);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperty);

            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {reportedProperty.GetSerializedString()} is {nameof(CommonClientResponseCodes.OK)} " +
                $"with a version of {updateResponse.Version}.");
        }

        // The callback to handle command invocation requests.
        private Task<CommandResponse> HandleCommandsAsync(CommandRequest commandRequest, object userContext)
        {
            // In this approach, we'll first switch through the component name returned and handle each component-level command.
            // For the "default" case, we'll first check if the component name is null.
            // If null, then this would be a top-level command request, so we'll switch through each top-level command.
            // If not null, then this is a component-level command that has not been implemented.

            // Switch through CommandRequest.ComponentName to handle all component-level commands.
            switch (commandRequest.ComponentName)
            {
                case Thermostat1:
                case Thermostat2:
                    // For each component, switch through CommandRequest.CommandName to handle the specific component-level command.
                    switch (commandRequest.CommandName)
                    {
                        case "getMaxMinReport":
                            return HandleMaxMinReportCommandAsync(commandRequest, userContext);

                        default:
                            _logger.LogWarning($"Received a command request that isn't" +
                            $" implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");

                            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                    }

                // For the default case, first check if CommandRequest.ComponentName is null.
                default:
                    // If CommandRequest.ComponentName is null, then this is a top-level command request.
                    if (commandRequest.ComponentName == null)
                    {
                        // Switch through CommandRequest.CommandName to handle all top-level commands.
                        switch (commandRequest.CommandName)
                        {
                            case "reboot":
                                return HandleRebootCommandAsync(commandRequest, userContext);

                            default:
                                _logger.LogWarning($"Received a command request that isn't" +
                                    $" implemented - command name = {commandRequest.CommandName}");

                                return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Received a command request that isn't" +
                            $" implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");

                        return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                    }
            }
        }

        // The callback to handle top-level "reboot" command.
        // This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private async Task<CommandResponse> HandleRebootCommandAsync(CommandRequest commandRequest, object userContext)
        {
            try
            {
                int delay = commandRequest.GetData<int>();

                _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
                await Task.Delay(delay * 1000);

                _temperature[Thermostat1] = _maxTemp[Thermostat1] = 0;
                _temperature[Thermostat2] = _maxTemp[Thermostat2] = 0;

                _temperatureReadingsDateTimeOffset.Clear();
                _logger.LogDebug($"Command: Reboot completed.");

                return new CommandResponse(CommonClientResponseCodes.OK);
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input for {commandRequest.CommandName} is invalid: {ex.Message}.");
                return new CommandResponse(CommonClientResponseCodes.BadRequest);
            }
        }

        // The callback to handle component-level "getMaxMinReport" command.
        // This method will returns the max, min and average temperature from the specified time to the current time.
        private Task<CommandResponse> HandleMaxMinReportCommandAsync(CommandRequest commandRequest, object userContext)
        {
            try
            {
                DateTimeOffset sinceInUtc = commandRequest.GetData<DateTimeOffset>();
                _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                    $"{sinceInUtc.LocalDateTime}.");

                if (_temperatureReadingsDateTimeOffset.ContainsKey(commandRequest.ComponentName))
                {
                    Dictionary<DateTimeOffset, double> allReadings = _temperatureReadingsDateTimeOffset[commandRequest.ComponentName];
                    Dictionary<DateTimeOffset, double> filteredReadings = allReadings.Where(i => i.Key > sinceInUtc)
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

                        _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\", MaxMinReport since {sinceInUtc.LocalDateTime}:" +
                            $" maxTemp={report.MaximumTemperature}, minTemp={report.MinimumTemperature}, avgTemp={report.AverageTemperature}, " +
                            $"startTime={report.StartTime.LocalDateTime}, endTime={report.EndTime.LocalDateTime}");

                        return Task.FromResult(new CommandResponse(report, CommonClientResponseCodes.OK));
                    }

                    _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\"," +
                        $" no relevant readings found since {sinceInUtc.LocalDateTime}, cannot generate any report.");

                    return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                }

                _logger.LogDebug($"Command: component=\"{commandRequest.ComponentName}\", no temperature readings sent yet," +
                    $" cannot generate any report.");

                return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogError($"Command input for {commandRequest.CommandName} is invalid: {ex.Message}.");

                return Task.FromResult(new CommandResponse(CommonClientResponseCodes.BadRequest));
            }
        }

        // Report the property values on "deviceInformation" component.
        // This is a component-level property update call.
        private async Task UpdateDeviceInformationPropertyAsync(CancellationToken cancellationToken)
        {
            const string componentName = "deviceInformation";
            var deviceInformationProperties = new Dictionary<string, object>
            {
                { "manufacturer", "element15" },
                { "model", "ModelIDxcdvmk" },
                { "swVersion", "1.0.0" },
                { "osName", "Windows 10" },
                { "processorArchitecture", "64-bit" },
                { "processorManufacturer", "Intel" },
                { "totalStorage", 256 },
                { "totalMemory", 1024 },
            };
            var deviceInformation = new ClientPropertyCollection();
            deviceInformation.AddComponentProperties(componentName, deviceInformationProperties);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(deviceInformation, cancellationToken);

            _logger.LogDebug($"Property: Update - component = '{componentName}', properties update is complete " +
                $"with a version of {updateResponse.Version}.");
        }

        // Send working set of device memory over telemetry.
        // This is a top-level telemetry call.
        private async Task SendDeviceMemoryTelemetryAsync(CancellationToken cancellationToken)
        {
            const string workingSetName = "workingSet";
            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            using var telemetryMessage = new TelemetryMessage
            {
                Telemetry = { [workingSetName] = workingSet },
            };

            await _deviceClient.SendTelemetryAsync(telemetryMessage, cancellationToken);

            _logger.LogDebug($"Telemetry: Sent - {telemetryMessage.Telemetry.GetSerializedString()} in KB.");
        }

        // Verify if the device has previously reported the current value for property "serialNumber".
        // If the expected value has not been previously reported then send device serial number over property update.
        // This is a top-level property update call.
        private async Task SendDeviceSerialNumberPropertyIfNotCurrentAsync(CancellationToken cancellationToken)
        {
            const string serialNumber = "serialNumber";
            const string currentSerialNumber = "SR-123456";

            // Verify if the device has previously reported the current value for property "serialNumber".
            // If the expected value has not been previously reported then report it.

            // Retrieve the device's properties.
            ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

            if (!properties.TryGetValue(serialNumber, out string serialNumberReported)
                || serialNumberReported != currentSerialNumber)
            {
                var reportedProperties = new ClientPropertyCollection();
                reportedProperties.AddRootProperty(serialNumber, currentSerialNumber);

                ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties, cancellationToken);

                _logger.LogDebug($"Property: Update - {reportedProperties.GetSerializedString()} is complete " +
                    $"with a version of {updateResponse.Version}.");
            }
        }

        // Send temperature updates over telemetry.
        // This also sends the value of max temperature since last reboot over property update.
        private async Task SendTemperatureAsync(string componentName, CancellationToken cancellationToken)
        {
            await SendTemperatureTelemetryAsync(componentName, cancellationToken);

            double maxTemp = _temperatureReadingsDateTimeOffset[componentName].Values.Max<double>();
            if (maxTemp > _maxTemp[componentName])
            {
                _maxTemp[componentName] = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync(componentName, cancellationToken);
            }
        }

        // Send temperature update over telemetry.
        // This is a component-level telemetry call.
        private async Task SendTemperatureTelemetryAsync(string componentName, CancellationToken cancellationToken)
        {
            const string telemetryName = "temperature";
            double currentTemperature = _temperature[componentName];

            using var telemtryMessage = new TelemetryMessage(componentName)
            {
                Telemetry = { [telemetryName] = currentTemperature },
            };

            await _deviceClient.SendTelemetryAsync(telemtryMessage, cancellationToken);

            _logger.LogDebug($"Telemetry: Sent - component=\"{componentName}\", {telemtryMessage.Telemetry.GetSerializedString()} in °C.");

            if (_temperatureReadingsDateTimeOffset.ContainsKey(componentName))
            {
                _temperatureReadingsDateTimeOffset[componentName].TryAdd(DateTimeOffset.UtcNow, currentTemperature);
            }
            else
            {
                _temperatureReadingsDateTimeOffset.TryAdd(
                    componentName,
                    new Dictionary<DateTimeOffset, double>
                    {
                        { DateTimeOffset.UtcNow, currentTemperature },
                    });
            }
        }

        // Send temperature over reported property update.
        // This is a component-level property update.
        private async Task UpdateMaxTemperatureSinceLastRebootAsync(string componentName, CancellationToken cancellationToken)
        {
            const string propertyName = "maxTempSinceLastReboot";
            double maxTemp = _maxTemp[componentName];
            var reportedProperties = new ClientPropertyCollection();
            reportedProperties.AddComponentProperty(componentName, propertyName, maxTemp);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties, cancellationToken);

            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {reportedProperties.GetSerializedString()}" +
                $" in °C is complete with a version of {updateResponse.Version}.");
        }

        private static double GenerateTemperatureWithinRange(int max = 50, int min = 0)
        {
            return Math.Round(s_random.NextDouble() * (max - min) + min, 1);
        }
    }
}
