// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
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

        private static readonly PayloadConvention s_payloadConvention = new CustomPayloadConvention();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        private static CancellationToken s_cancellationToken;
        private static DateTimeOffset s_applicationStartTime;

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this
        // information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<string, Dictionary<DateTimeOffset, double>> _temperatureReadingsDateTimeOffset =
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

            s_cancellationToken = cancellationToken;

            var t = await _deviceClient.GetTwinAsync(s_cancellationToken);

            Properties properties = await _deviceClient.GetPropertiesAsync(s_payloadConvention, s_cancellationToken);

            // see if we have a writable property request for "serialNumber" and Thermostat1."targetTemperature".
            string serialNumber = "serialNumber";
            if (properties.Writable.Contains(serialNumber))
            {
                _logger.LogDebug($"Found writable property request \"{serialNumber}\": {properties.Writable[serialNumber]}");
            }

            string targetTemperature = "targetTemperature";
            if (properties.Writable.Contains(Thermostat1) && ((JsonElement)properties[Thermostat1]).TryGetProperty(targetTemperature, out JsonElement targetTemperatureThermostat2))
            {
                _logger.LogDebug($"Found writable property request \"{Thermostat2}-{targetTemperature}\": {targetTemperatureThermostat2}");
            }

            // see if we have a device reported value for "serialNumber" and Thermostat2."initialValue".
            if (properties.Contains("serialNumber"))
            {
                _logger.LogDebug($"Found property \"serialNumber\": {properties["serialNumber"]}");
            }

            string initialValue = "initialValue";
            if (properties.Contains(Thermostat2) && ((JsonElement)properties[Thermostat2]).TryGetProperty(initialValue, out JsonElement initialStateThermostat2))
            {
                _logger.LogDebug($"Found property \"{Thermostat2}-{initialValue}\": {initialStateThermostat2}");
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
            await _deviceClient.SubscribeToCommandsAsync(CommandEventDispatcherAsync, null, s_payloadConvention, s_cancellationToken);

            _logger.LogDebug("Set handler to receive writable property updates.");
            await _deviceClient.SubscribeToWritablePropertyEventAsync(WritablePropertyEventDispatcherAsync, null, s_payloadConvention, s_cancellationToken);

            await UpdateDeviceInformationAsync(s_cancellationToken);
            await SendDeviceSerialNumberAsync(s_cancellationToken);

            await SendInitialPropertyUpdatesAsync(Thermostat2, s_cancellationToken);

            bool temperatureReset = true;
            _maxTemp[Thermostat1] = 0d;
            _maxTemp[Thermostat2] = 0d;

            while (!s_cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading for each "Thermostat" component.
                    _temperature[Thermostat1] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                    _temperature[Thermostat2] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                }

                await SendTemperatureAsync(Thermostat1, s_cancellationToken);
                await SendTemperatureAsync(Thermostat2, s_cancellationToken);
                await SendDeviceMemoryAsync(s_cancellationToken);
                await SendDeviceHealthTelemetryAsync(s_cancellationToken);

                temperatureReset = _temperature[Thermostat1] == 0 && _temperature[Thermostat2] == 0;
                await Task.Delay(5 * 1000, CancellationToken.None);
            }
        }

        private async Task SendInitialPropertyUpdatesAsync(string componentName, CancellationToken cancellationToken)
        {
            const string initialValueName = "initialValue";
            var initialValue = new ThermostatInitialValue()
            {
                Temperature = 55,
                Humidity = 68
            };

            var propertyPatch = new PropertyCollection(s_payloadConvention);
            propertyPatch.Add(initialValueName, initialValue, componentName);

            await _deviceClient.UpdatePropertiesAsync(propertyPatch, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{initialValueName}\" is complete.");
        }

        // Send the device health status over telemetry.
        private async Task SendDeviceHealthTelemetryAsync(CancellationToken cancellationToken)
        {
            string componentName = Thermostat1;
            string deviceHealthName = "deviceHealth";
            var deviceHealth = new DeviceHealth
            {
                Status = "Running",
                RunningTimeInSeconds = s_stopwatch.Elapsed.TotalSeconds,
                IsStopRequested = false,
                StartTime = s_applicationStartTime,
            };

            using var message = new TelemetryMessage(componentName)
            {
                Telemetry = new TelemetryCollection(s_payloadConvention)
                {
                    [deviceHealthName] = deviceHealth,
                },
            };

            // Something ...(just for demonstartion; this isn't a part of the model).
            message.Telemetry.Add("another value", 56.2);

            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerailizedString()}.");
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

                return new CommandResponse(updateTemperatureResponse, (int)StatusCode.Completed, s_payloadConvention);
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

                        return Task.FromResult(new CommandResponse(report, (int)StatusCode.Completed, s_payloadConvention));
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

        private async Task WritablePropertyEventDispatcherAsync(PropertyCollection writableProperties, object userContext)
        {
            foreach (KeyValuePair<string, object> propertyUpdate in writableProperties)
            {
                // The dispatcher key will be either the root-level property name or the component name.
                switch (propertyUpdate.Key)
                {
                    case "temperatureRange":
                        await SendTemperatureRangeAsync(writableProperties, null, propertyUpdate.Key);
                        break;

                    // Component level properties will be available under a nested dictionary
                    case Thermostat1:
                        Dictionary<string, object> thermostat1Properties =
                            s_payloadConvention.PayloadSerializer.DeserializeToType<Dictionary<string, object>>(((JsonElement)propertyUpdate.Value).GetRawText());
                        foreach (KeyValuePair<string, object> componentPropertyUpdate in thermostat1Properties)
                        {
                            switch (componentPropertyUpdate.Key)
                            {
                                case "targetTemperature":
                                    await TargetTemperatureUpdateCallbackAsync(writableProperties, null, propertyUpdate.Key);
                                    break;

                                case "humidityRange":
                                    await SendHumidityRangeAsync(writableProperties, null, propertyUpdate.Key);
                                    break;

                                default:
                                    _logger.LogDebug($"Property: Received a property update for component \"{Thermostat1}\" that is not implemented.");
                                    break;
                            }
                        }
                        break;

                    case Thermostat2:
                        Dictionary<string, object> thermostat2Properties =
                            s_payloadConvention.PayloadSerializer.DeserializeToType<Dictionary<string, object>>(((JsonElement)propertyUpdate.Value).GetRawText());
                        foreach (KeyValuePair<string, object> componentPropertyUpdate in thermostat2Properties)
                        {
                            switch (componentPropertyUpdate.Key)
                            {
                                case "targetTemperature":
                                    await TargetTemperatureUpdateCallbackAsync(writableProperties, null, propertyUpdate.Key);
                                    break;

                                default:
                                    _logger.LogDebug($"Property: Received a property update for component \"{Thermostat2}\" that is not implemented.");
                                    break;
                            }
                        }
                        break;

                    default:
                        _logger.LogDebug($"Property: Received a property update that is not implemented.");
                        break;
                }
            }
        }

        private async Task SendTemperatureRangeAsync(PropertyCollection writableProperties, object userContext, string dispatcherKey)
        {
            string propertyName = dispatcherKey;

            TemperatureRange temperatureRangeDesired = writableProperties.GetValue<TemperatureRange>(propertyName);

            var temperatureUpdateResponse = new CustomWritablePropertyResponse(
                temperatureRangeDesired,
                (int)StatusCode.Completed,
                writableProperties.Version,
                "The operation completed successfully.");

            var propertyPatch = new PropertyCollection(s_payloadConvention)
            {
                [propertyName] = temperatureUpdateResponse,
            };

            await _deviceClient.UpdatePropertiesAsync(propertyPatch, s_cancellationToken);
            _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerailizedString()}\" is complete.");
        }

        private async Task SendHumidityRangeAsync(PropertyCollection writableProperties, object userContext, string dispatcherKey)
        {
            string componentName = dispatcherKey;
            string propertyName = "humidityRange";

            bool humidityRangeReceived = ((JsonElement)writableProperties[componentName]).TryGetProperty(propertyName, out JsonElement humidityRangeJson);

            if (!humidityRangeReceived)
            {
                _logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.\n{writableProperties.Collection}");
                return;
            }

            HumidityRange humidityRangeDesired = s_payloadConvention.PayloadSerializer.DeserializeToType<HumidityRange>(humidityRangeJson.GetRawText());

            var humidityRangeResponse = new CustomWritablePropertyResponse(
                humidityRangeDesired,
                (int)StatusCode.Completed,
                writableProperties.Version,
                "The operation completed successfully.");

            var propertyPatch = new PropertyCollection(s_payloadConvention)
            {
                [propertyName] = humidityRangeResponse,
            };

            await _deviceClient.UpdatePropertiesAsync(propertyPatch, s_cancellationToken);
            _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerailizedString()}\" is complete.");
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and property update.
        // This callback is invoked for all updates received for the two associated components.
        private async Task TargetTemperatureUpdateCallbackAsync(PropertyCollection writableProperties, object userContext, string dispatcherKey)
        {
            string componentName = dispatcherKey;
            const string propertyName = "targetTemperature";

            bool targetTempUpdateReceived = ((JsonElement)writableProperties[componentName]).TryGetProperty(propertyName, out JsonElement targetTemperatureJson);

            if (!targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.\n{writableProperties.Collection}");
                return;
            }

            double targetTemperature = targetTemperatureJson.GetDouble();
            _logger.LogDebug($"Property: Received - component=\"{componentName}\", {{ \"{propertyName}\": {targetTemperature}°C }}.");

            var pendingReportedProperty = new WritablePropertyResponse(
                targetTemperature,
                (int)StatusCode.InProgress,
                writableProperties.Version);

            var pendingPropertyPatch = new PropertyCollection();
            pendingPropertyPatch.Add(propertyName, pendingReportedProperty, componentName);

            await _deviceClient.UpdatePropertiesAsync(pendingPropertyPatch, s_cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {targetTemperature} }} in °C is {StatusCode.InProgress}.");

            // Update Temperature in 2 steps
            double step = (targetTemperature - _temperature[componentName]) / 2d;
            for (int i = 1; i <= 2; i++)
            {
                _temperature[componentName] = Math.Round(_temperature[componentName] + step, 1);
                await Task.Delay(6 * 1000);
            }

            var completedReportedProperty = new WritablePropertyResponse(
                _temperature[componentName],
                (int)StatusCode.Completed,
                writableProperties.Version,
                "Successfully updated target temperature");

            var completePropertyPatch = new PropertyCollection();
            completePropertyPatch.Add(propertyName, completedReportedProperty, componentName);

            await _deviceClient.UpdatePropertiesAsync(completePropertyPatch, s_cancellationToken);
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

            var propertyPatch = new PropertyCollection();
            propertyPatch.Add(deviceInformation, componentName);

            await _deviceClient.UpdatePropertiesAsync(propertyPatch, cancellationToken);
            _logger.LogDebug($"Property: Update - component = '{componentName}', properties update is complete.");
        }

        // Send working set of device memory over telemetry.
        private async Task SendDeviceMemoryAsync(CancellationToken cancellationToken)
        {
            const string workingSetName = "workingSet";

            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;

            using var message = new TelemetryMessage
            {
                Telemetry = { [workingSetName] = workingSet },
            };

            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerailizedString()} in KB.");
        }

        // Send device serial number over property update.
        private async Task SendDeviceSerialNumberAsync(CancellationToken cancellationToken)
        {
            const string propertyName = "serialNumber";

            var propertyCollection = new PropertyCollection
            {
                [propertyName] = SerialNumber
            };

            await _deviceClient.UpdatePropertiesAsync(propertyCollection, cancellationToken);
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

            // We can do a direct initialization like this...
            using var message = new TelemetryMessage()
            {
                ComponentName = componentName,
                Telemetry = {
                    [temperatureName] = currentTemperature,
                },
                Properties = { ["myCustomProperty"] = "A custom property" }
            };

            // Or set the property this way
            using var messageCustom = new TelemetryMessage
            {
                ComponentName = componentName,
                Telemetry = new TelemetryCollection(s_payloadConvention)
                {
                    [temperatureName] = currentTemperature,
                }
            };
            messageCustom.Telemetry["somethingElse"] = 4;
            messageCustom.Telemetry.Add("anotherName", 42);

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

            var propertyPatch = new PropertyCollection();
            propertyPatch.Add(propertyName, maxTemp, componentName);
            await _deviceClient.UpdatePropertiesAsync(propertyPatch, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{ \"{propertyName}\": {maxTemp} }} in °C is complete.");
        }
    }
}
