﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnpHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404,
        BadRequest = 400
    }

    public class TemperatureControllerSample
    {
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";
        private const string SerialNumber = "SR-123456";

        private static readonly Random s_random = new Random();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this
        // information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<string, Dictionary<DateTimeOffset, double>> _temperatureReadingsDateTimeOffset =
            new Dictionary<string, Dictionary<DateTimeOffset, double>>();

        // A dictionary to hold all desired property change callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the componentName.
        private readonly IDictionary<string, DesiredPropertyUpdateCallback> _desiredPropertyUpdateCallbacks =
            new Dictionary<string, DesiredPropertyUpdateCallback>();

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
            // This sample follows the following workflow:
            // -> Set handler to receive "reboot" command - root interface.
            // -> Set handler to receive "getMaxMinReport" command - on "Thermostat" components.
            // -> Set handler to receive "targetTemperature" property updates from service - on "Thermostat" components.
            // -> Update device information on "deviceInformation" component.
            // -> Send initial device info - "workingSet" over telemetry, "serialNumber" over reported property update - root interface.
            // -> Periodically send "temperature" over telemetry - on "Thermostat" components.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set - on "Thermostat" components.

            _logger.LogDebug("Set handler for 'reboot' command.");
            await _deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, _deviceClient, cancellationToken);

            // For a component-level command, the command name is in the format "<component-name>*<command-name>".
            _logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await _deviceClient.SetMethodHandlerAsync("thermostat1*getMaxMinReport", HandleMaxMinReportCommand, Thermostat1, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("thermostat2*getMaxMinReport", HandleMaxMinReportCommand, Thermostat2, cancellationToken);

            _logger.LogDebug("Set handler to receive 'targetTemperature' updates.");
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(SetDesiredPropertyUpdateCallback, null, cancellationToken);
            _desiredPropertyUpdateCallbacks.Add(Thermostat1, TargetTemperatureUpdateCallbackAsync);
            _desiredPropertyUpdateCallbacks.Add(Thermostat2, TargetTemperatureUpdateCallbackAsync);

            await UpdateDeviceInformationAsync(cancellationToken);
            await SendDeviceSerialNumberAsync(cancellationToken);

            bool temperatureReset = true;
            _maxTemp[Thermostat1] = 0d;
            _maxTemp[Thermostat2] = 0d;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading for each "Thermostat" component.
                    _temperature[Thermostat1] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                    _temperature[Thermostat2] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                }

                await SendTemperatureAsync(Thermostat1, cancellationToken);
                await SendTemperatureAsync(Thermostat2, cancellationToken);
                await SendDeviceMemoryAsync(cancellationToken);

                temperatureReset = _temperature[Thermostat1] == 0 && _temperature[Thermostat2] == 0;
                await Task.Delay(5 * 1000);
            }
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            try
            {
                int delay = JsonConvert.DeserializeObject<int>(request.DataAsJson);

                _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
                await Task.Delay(delay * 1000);

                _temperature[Thermostat1] = _maxTemp[Thermostat1] = 0;
                _temperature[Thermostat2] = _maxTemp[Thermostat2] = 0;

                _temperatureReadingsDateTimeOffset.Clear();
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return new MethodResponse((int)StatusCode.BadRequest);
            }

            return new MethodResponse((int)StatusCode.Completed);
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the
        // specified time to the current time.
        private Task<MethodResponse> HandleMaxMinReportCommand(MethodRequest request, object userContext)
        {
            try
            {
                string componentName = (string)userContext;
                DateTime sinceInUtc = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
                var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);

                if (_temperatureReadingsDateTimeOffset.ContainsKey(componentName))
                {
                    _logger.LogDebug($"Command: Received - component=\"{componentName}\", generating max, min and avg temperature " +
                        $"report since {sinceInDateTimeOffset.LocalDateTime}.");

                    Dictionary<DateTimeOffset, double> allReadings = _temperatureReadingsDateTimeOffset[componentName];
                    Dictionary<DateTimeOffset, double> filteredReadings = allReadings.Where(i => i.Key > sinceInDateTimeOffset)
                        .ToDictionary(i => i.Key, i => i.Value);

                    if (filteredReadings != null && filteredReadings.Any())
                    {
                        var report = new
                        {
                            maxTemp = filteredReadings.Values.Max<double>(),
                            minTemp = filteredReadings.Values.Min<double>(),
                            avgTemp = filteredReadings.Values.Average(),
                            startTime = filteredReadings.Keys.Min(),
                            endTime = filteredReadings.Keys.Max(),
                        };

                        _logger.LogDebug($"Command: component=\"{componentName}\", MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                            $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime.LocalDateTime}, " +
                            $"endTime={report.endTime.LocalDateTime}");

                        byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                        return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                    }

                    _logger.LogDebug($"Command: component=\"{componentName}\", no relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, " +
                        $"cannot generate any report.");
                    return Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
                }

                _logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
                return Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        private Task SetDesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
        {
            bool callbackNotInvoked = true;

            foreach (KeyValuePair<string, object> propertyUpdate in desiredProperties)
            {
                string componentName = propertyUpdate.Key;
                if (_desiredPropertyUpdateCallbacks.ContainsKey(componentName))
                {
                    _desiredPropertyUpdateCallbacks[componentName]?.Invoke(desiredProperties, componentName);
                    callbackNotInvoked = false;
                }
            }

            if (callbackNotInvoked)
            {
                _logger.LogDebug($"Property: Received a property update that is not implemented by any associated component.");
            }

            return Task.CompletedTask;
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and property update.
        private async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            const string propertyName = "targetTemperature";
            string componentName = (string)userContext;

            bool targetTempUpdateReceived = PnpConvention.TryGetPropertyFromTwin(
                desiredProperties,
                propertyName,
                out double targetTemperature,
                componentName);
            if (!targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.\n{desiredProperties.ToJson()}");
                return;
            }

            _logger.LogDebug($"Property: Received - component=\"{componentName}\", {{ \"{propertyName}\": {targetTemperature}°C }}.");

            TwinCollection pendingReportedProperty = PnpConvention.CreateComponentWritablePropertyResponse(
                componentName,
                propertyName,
                targetTemperature,
                (int)StatusCode.InProgress,
                desiredProperties.Version);

            await _deviceClient.UpdateReportedPropertiesAsync(pendingReportedProperty);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {targetTemperature} }} in °C is {StatusCode.InProgress}.");

            // Update Temperature in 2 steps
            double step = (targetTemperature - _temperature[componentName]) / 2d;
            for (int i = 1; i <= 2; i++)
            {
                _temperature[componentName] = Math.Round(_temperature[componentName] + step, 1);
                await Task.Delay(6 * 1000);
            }

            TwinCollection completedReportedProperty = PnpConvention.CreateComponentWritablePropertyResponse(
                componentName,
                propertyName,
                _temperature[componentName],
                (int)StatusCode.Completed,
                desiredProperties.Version,
                "Successfully updated target temperature");

            await _deviceClient.UpdateReportedPropertiesAsync(completedReportedProperty);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {_temperature[componentName]} }} in °C is {StatusCode.Completed}");
        }

        // Report the property updates on "deviceInformation" component.
        private async Task UpdateDeviceInformationAsync(CancellationToken cancellationToken)
        {
            const string componentName = "deviceInformation";

            TwinCollection deviceInfoTc = PnpConvention.CreateComponentPropertyPatch(
                componentName,
                new Dictionary<string, object>
                {
                    { "manufacturer", "element15" },
                    { "model", "ModelIDxcdvmk" },
                    { "swVersion", "1.0.0" },
                    { "osName", "Windows 10" },
                    { "processorArchitecture", "64-bit" },
                    { "processorManufacturer", "Intel" },
                    { "totalStorage", 256 },
                    { "totalMemory", 1024 },
                });

            await _deviceClient.UpdateReportedPropertiesAsync(deviceInfoTc, cancellationToken);
            _logger.LogDebug($"Property: Update - component = '{componentName}', properties update is complete.");
        }

        // Send working set of device memory over telemetry.
        private async Task SendDeviceMemoryAsync(CancellationToken cancellationToken)
        {
            const string workingSetName = "workingSet";

            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;

            var telemetry = new Dictionary<string, object>
            {
                { workingSetName, workingSet },
            };

            using Message msg = PnpConvention.CreateMessage(telemetry);

            await _deviceClient.SendEventAsync(msg, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {JsonConvert.SerializeObject(telemetry)} in KB.");
        }

        // Send device serial number over property update.
        private async Task SendDeviceSerialNumberAsync(CancellationToken cancellationToken)
        {
            const string propertyName = "serialNumber";
            TwinCollection reportedProperties = PnpConvention.CreatePropertyPatch(propertyName, SerialNumber);

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            _logger.LogDebug($"Property: Update - {{ \"{propertyName}\": \"{SerialNumber}\" }} is complete.");
        }

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

        private async Task SendTemperatureTelemetryAsync(string componentName, CancellationToken cancellationToken)
        {
            const string telemetryName = "temperature";
            double currentTemperature = _temperature[componentName];
            using Message msg = PnpConvention.CreateMessage(telemetryName, currentTemperature, componentName);

            await _deviceClient.SendEventAsync(msg, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - component=\"{componentName}\", {{ \"{telemetryName}\": {currentTemperature} }} in °C.");

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

        private async Task UpdateMaxTemperatureSinceLastRebootAsync(string componentName, CancellationToken cancellationToken)
        {
            const string propertyName = "maxTempSinceLastReboot";
            double maxTemp = _maxTemp[componentName];
            TwinCollection reportedProperties = PnpConvention.CreateComponentPropertyPatch(componentName, propertyName, maxTemp);

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{ \"{propertyName}\": {maxTemp} }} in °C is complete.");
        }
    }
}
