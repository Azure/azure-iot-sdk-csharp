﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private static readonly Random s_random = new();
        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

        private static readonly ObjectSerializer s_objectSerializer = new CustomObjectSerializer();
        private static readonly TelemetryConvention s_customTelemetryConvention = new CustomTelemetryConvention();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        private static DateTimeOffset s_applicationStartTime;

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this
        // information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<string, Dictionary<DateTimeOffset, double>> _temperatureReadingsDateTimeOffset =
            new();

        // A dictionary to hold all desired property change callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the componentName/ root-level property name.
        private readonly Dictionary<string, Func<PropertyCollection, object, string, Task>> _writablePropertyEventCallbacks =
            new();

        // A dictionary to hold all command callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the root-level command name/ {<componentName>*<commandName>}.
        private readonly Dictionary<string, Func<CommandRequest, object, Task<CommandResponse>>> _commandEventCallbacks =
            new();

        // Dictionary to hold the current temperature for each "Thermostat" component.
        private readonly Dictionary<string, double> _temperature = new();

        // Dictionary to hold the max temperature since last reboot, for each "Thermostat" component.
        private readonly Dictionary<string, double> _maxTemp = new();

        public TemperatureControllerSample(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");

            if (logger == null)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builer => builer.AddConsole());
                _logger = loggerFactory.CreateLogger<TemperatureControllerSample>();
            }
            else
            {
                _logger = logger;
            }
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

            var t = await _deviceClient.GetTwinAsync(cancellationToken);

            Properties properties = await _deviceClient.GetPropertiesAsync(cancellationToken);

            // see if we have a writable property request for "serialNumber"
            string writablePropertyName = "serialNumber";
            if (properties.Writable.Contains(writablePropertyName))
            {
                _logger.LogDebug($"Found writable property request \"{writablePropertyName}\": {properties.Writable[writablePropertyName]}");
            }

            // see if we have a device reported value for "serialNumber" and Thermostat2."initialValue".
            if (properties.Contains("serialNumber"))
            {
                var serialNumberReported = properties["serialNumber"];
            }

            if (properties.Contains(Thermostat2) && properties[Thermostat2].ContainsKey("initialValue"))
            {
                var initialStateThermostat2 = properties[Thermostat2]["initialValue"];
            }

            s_applicationStartTime = DateTimeOffset.Now;

            // CommandEventDispatcherAsync is a dispatcher that we provide to dispatch the individual component/ root-level command callbacks.
            // Alternatively, you can also have an uber callback that implements a dispatcher internally.
            // The important thing to note here is that you can only set a single callback for subscribing to command events.
            // If you set multiple callbacks to this API, the latest one will be invoked.
            _logger.LogDebug("Set handler for 'reboot', 'getMaxMinReport' and 'updateTemperatureWithDelay' commands.");
            _commandEventCallbacks.Add("reboot", HandleRebootCommandAsync);
            _commandEventCallbacks.Add("getMaxMinReport", HandleMaxMinReportCommandAsync);
            _commandEventCallbacks.Add("updateTemperatureWithDelay", HandleTemperatureUpdateCommandAsync);
            await _deviceClient.SubscribeToCommandsAsync(CommandEventDispatcherAsync, null, objectSerializer: s_objectSerializer, cancellationToken: cancellationToken);

            // WritablePropertyEventDispatcherAsync is a dispatcher that we provide to dispatch the individual component/ root-level property callbacks.
            // Alternatively, you can also have an uber callback that implements a dispatcher internally.
            // The important thing to note here is that you can only set a single callback for subscribing to property updates.
            // If you set multiple callbacks to this API, the latest one will be invoked.
            _logger.LogDebug("Set handler to receive 'targetTemperature' and 'temperatureRange' updates.");
            _writablePropertyEventCallbacks.Add(Thermostat1, TargetTemperatureUpdateCallbackAsync);
            _writablePropertyEventCallbacks.Add(Thermostat2, TargetTemperatureUpdateCallbackAsync);
            _writablePropertyEventCallbacks.Add("temperatureRange", SendTemperatureRangeAsync);
            await _deviceClient.SubscribeToWritablePropertyEventAsync(WritablePropertyEventDispatcherAsync, userContext: cancellationToken, cancellationToken: cancellationToken);

            await UpdateDeviceInformationAsync(cancellationToken);
            await SendDeviceSerialNumberAsync(cancellationToken);

            await SendInitialPropertyUpdatesAsync(Thermostat2, cancellationToken);

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
                await SendDeviceHealthTelemetryAsync(Thermostat1, cancellationToken);

                temperatureReset = _temperature[Thermostat1] == 0 && _temperature[Thermostat2] == 0;
                await Task.Delay(5 * 1000, cancellationToken);
            }
        }

        private async Task SendInitialPropertyUpdatesAsync(string componentName, CancellationToken cancellationToken)
        {
            const string initialValueName = "initialValue";
            var readonlyPropertyPatch = new ThermostatInitialValue()
            {
                Temperature = 55,
                Humidity = 68
            };

            await _deviceClient.UpdatePropertyAsync(initialValueName, readonlyPropertyPatch, componentName, s_objectSerializer, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{initialValueName}\" is complete.");
        }

        // Send the device health status over telemetry.
        private async Task SendDeviceHealthTelemetryAsync(string componentName, CancellationToken cancellationToken)
        {
            string deviceHealthName = "deviceHealth";
            var deviceHealth = new DeviceHealth
            {
                Status = "Running",
                RunningTimeInSeconds = s_stopwatch.Elapsed.TotalSeconds,
                IsStopRequested = false,
                StartTime = s_applicationStartTime,
            };

            IDictionary<string, object> telemetryPayload = TelemetryConvention.FormatTelemetryPayload(deviceHealthName, deviceHealth);

            using var message = new Message(telemetryPayload, s_customTelemetryConvention)
            {
                Properties = { ["property1"] = "myValue" },
                ComponentName = componentName,
            };

            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {s_customTelemetryConvention.SerializeToString(telemetryPayload)}.");
        }

        private Task<CommandResponse> CommandEventDispatcherAsync(CommandRequest commandRequest, object userContext)
        {
            // Ideally you'd need to use the combination of command name and component name to identify the command event,
            // but for the purpose of this sample we can use the command name by itself since we know that all command names are unique.
            string dispatcherKey = commandRequest.Name;
            if (_commandEventCallbacks.ContainsKey(dispatcherKey))
            {
                return _commandEventCallbacks[dispatcherKey]?.Invoke(commandRequest, userContext);
            }

            _logger.LogDebug($"Command: Received a command request that is not implemented.");
            return Task.FromResult(new CommandResponse((int)StatusCode.NotFound));
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private async Task<CommandResponse> HandleRebootCommandAsync(CommandRequest request, object userContext)
        {
            try
            {
                int delay = request.GetData<int>();

                _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
                await Task.Delay(TimeSpan.FromSeconds(delay));

                _temperature[Thermostat1] = _maxTemp[Thermostat1] = 0;
                _temperature[Thermostat2] = _maxTemp[Thermostat2] = 0;

                _temperatureReadingsDateTimeOffset.Clear();
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return new CommandResponse((int)StatusCode.BadRequest);
            }

            return new CommandResponse((int)StatusCode.Completed);
        }

        private async Task<CommandResponse> HandleTemperatureUpdateCommandAsync(CommandRequest request, object userContext)
        {
            try
            {
                UpdateTemperatureRequest updateTemperatureRequest = request.GetData<UpdateTemperatureRequest>();

                _logger.LogDebug($"Command: Received - component=\"{request.ComponentName}\"," +
                    $" updating temperature reading to {updateTemperatureRequest.TargetTemperature}°C after {updateTemperatureRequest.Delay} seconds).");
                await Task.Delay(TimeSpan.FromSeconds(updateTemperatureRequest.Delay));

                _temperature[request.ComponentName] = updateTemperatureRequest.TargetTemperature;

                var updateTemperatureResponse = new UpdateTemperatureResponse
                {
                    TargetTemperature = updateTemperatureRequest.TargetTemperature,
                    Status = (int)StatusCode.Completed
                };

                _logger.LogDebug($"Command: component=\"{request.ComponentName}\", target temperature {updateTemperatureResponse.TargetTemperature}°C" +
                            $" has {StatusCode.Completed}.");

                return new CommandResponse(updateTemperatureResponse, (int)StatusCode.Completed, s_objectSerializer);
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return new CommandResponse((int)StatusCode.BadRequest);
            }
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the
        // specified time to the current time.
        private Task<CommandResponse> HandleMaxMinReportCommandAsync(CommandRequest request, object userContext)
        {
            try
            {
                string componentName = request.ComponentName;
                DateTime sinceInUtc = request.GetData<DateTime>();
                var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);

                if (_temperatureReadingsDateTimeOffset.ContainsKey(componentName))
                {
                    _logger.LogDebug($"Command: Received - component=\"{componentName}\", generating max, min and avg temperature " +
                        $"report since {sinceInDateTimeOffset.LocalDateTime}.");

                    Dictionary<DateTimeOffset, double> allReadings = _temperatureReadingsDateTimeOffset[componentName];
                    var filteredReadings = allReadings.Where(i => i.Key > sinceInDateTimeOffset)
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

                        _logger.LogDebug($"Command: component=\"{componentName}\", MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                            $" maxTemp={report.MaximumTemperature}°C, minTemp={report.MinimumTemperature}°C, avgTemp={report.AverageTemperature}°C," +
                            $" startTime={report.StartTime.LocalDateTime}, endTime={report.EndTime.LocalDateTime}");

                        return Task.FromResult(new CommandResponse(report, (int)StatusCode.Completed, s_objectSerializer));
                    }

                    _logger.LogDebug($"Command: component=\"{componentName}\", no relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, " +
                        $"cannot generate any report.");
                    return Task.FromResult(new CommandResponse((int)StatusCode.NotFound));
                }

                _logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
                return Task.FromResult(new CommandResponse((int)StatusCode.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new CommandResponse((int)StatusCode.BadRequest));
            }
        }

        private Task WritablePropertyEventDispatcherAsync(PropertyCollection desiredProperties, object cancellationToken)
        {
            foreach (KeyValuePair<string, object> propertyUpdate in desiredProperties)
            {
                // The dispatcher key will be either the root-level property name or the component name.
                string dispatcherKey = propertyUpdate.Key;
                if (_writablePropertyEventCallbacks.ContainsKey(dispatcherKey))
                {
                    return _writablePropertyEventCallbacks[dispatcherKey]?.Invoke(desiredProperties, cancellationToken, dispatcherKey);
                }
            }

            _logger.LogDebug($"Property: Received a property update that is not implemented.");
            return Task.CompletedTask;
        }

        private async Task SendTemperatureRangeAsync(PropertyCollection desiredProperties, object userContext, string dispatcherKey)
        {
            string propertyName = dispatcherKey;
            var cancellationToken = (CancellationToken)userContext;

            // PropertyCollection.Value is now always JObject (since we create PropertyCollection from TwinCollection).
            // This implementation detail will need to be addressed.
            string serializedProperties = ((JObject)desiredProperties[propertyName]).ToString();
            TemperatureRange temeratureRangeDesired = s_objectSerializer.DeserializeToType<TemperatureRange>(serializedProperties);

            var writablePropertyResponse = new WritablePropertyResponse(temeratureRangeDesired, s_objectSerializer)
            {
                AckCode = (int)StatusCode.Completed,
                AckVersion = desiredProperties.Version,
                AckDescription = "The operation completed successfully."
            };

            await _deviceClient.UpdateWritablePropertyAsync(propertyName, writablePropertyResponse, cancellationToken: cancellationToken);
            _logger.LogDebug($"Property: Update - {{\"{propertyName}\" is complete.");
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and property update.
        // This callback is invoked for all updates received for the two associated components.
        private async Task TargetTemperatureUpdateCallbackAsync(PropertyCollection desiredProperties, object userContext, string dispatcherKey)
        {
            string componentName = dispatcherKey;
            var cancellationToken = (CancellationToken)userContext;

            // The component has a single writable property, so the property name is a const here.
            // Otherwise this would have been a switch-case.
            const string propertyName = "targetTemperature";

            // PropertyCollection.Value is now always JObject (since we create PropertyCollection from TwinCollection).
            // This implementation detail will need to be addressed.
            bool targetTempUpdateReceived = ((JObject)desiredProperties[componentName]).ContainsKey(propertyName);

            if (!targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.\n{desiredProperties.ToJson()}");
                return;
            }

            double targetTemperature = desiredProperties[componentName][propertyName];
            _logger.LogDebug($"Property: Received - component=\"{componentName}\", {{ \"{propertyName}\": {targetTemperature}°C }}.");

            var pendingReportedProperty = new WritablePropertyResponse(targetTemperature)
            {
                AckCode = (int)StatusCode.InProgress,
                AckVersion = desiredProperties.Version
            };

            await _deviceClient.UpdateWritablePropertyAsync(propertyName, pendingReportedProperty, componentName, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {targetTemperature} }} in °C is {StatusCode.InProgress}.");

            // Update Temperature in 2 steps
            double step = (targetTemperature - _temperature[componentName]) / 2d;
            for (int i = 1; i <= 2; i++)
            {
                _temperature[componentName] = Math.Round(_temperature[componentName] + step, 1);
                await Task.Delay(6 * 1000);
            }

            var completedReportedProperty = new WritablePropertyResponse(_temperature[componentName])
            {
                AckCode = (int)StatusCode.Completed,
                AckVersion = desiredProperties.Version,
                AckDescription = "Successfully updated target temperature"
            };

            await _deviceClient.UpdateWritablePropertyAsync(propertyName, completedReportedProperty, componentName, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {_temperature[componentName]} }} in °C is {StatusCode.Completed}");
        }

        // Report the property updates on "deviceInformation" component.
        private async Task UpdateDeviceInformationAsync(CancellationToken cancellationToken)
        {
            const string componentName = "deviceInformation";

            var deviceInformation = new Dictionary<string, object>
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

            await _deviceClient.UpdatePropertiesAsync(deviceInformation, componentName, cancellationToken: cancellationToken);
            _logger.LogDebug($"Property: Update - component = '{componentName}', properties update is complete.");
        }

        // Send working set of device memory over telemetry.
        private async Task SendDeviceMemoryAsync(CancellationToken cancellationToken)
        {
            const string workingSetName = "workingSet";

            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            IDictionary<string, object> telemetryPayload = TelemetryConvention.FormatTelemetryPayload(workingSetName, workingSet);
            using var message = new Message(telemetryPayload);

            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {TelemetryConvention.Instance.SerializeToString(telemetryPayload)} in KB.");
        }

        // Send device serial number over property update.
        private async Task SendDeviceSerialNumberAsync(CancellationToken cancellationToken)
        {
            const string propertyName = "serialNumber";
            await _deviceClient.UpdatePropertyAsync(propertyName, SerialNumber, cancellationToken: cancellationToken);
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
            const string temperatureName = "temperature";
            double currentTemperature = _temperature[componentName];

            IDictionary<string, object> telemetryPayload = TelemetryConvention.FormatTelemetryPayload(temperatureName, currentTemperature);
            using var message = new Message(telemetryPayload);

            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - component=\"{componentName}\", {{ \"{temperatureName}\": {currentTemperature} }} in °C.");

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

            await _deviceClient.UpdatePropertyAsync(propertyName, maxTemp, componentName, cancellationToken: cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{ \"{propertyName}\": {maxTemp} }} in °C is complete.");
        }
    }
}
