// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnpHelpers;

namespace TemperatureController
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404
    }

    public class Program
    {
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/TemperatureController.json
        private const string ModelId = "dtmi:com:example:TemperatureController;1";

        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";
        private const string SerialNumber = "SR-123456";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;
        private static ILogger s_logger;

        // A dictionary to hold all desired property change callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the componentName.
        private static readonly IDictionary<string, DesiredPropertyUpdateCallback> s_desiredPropertyUpdateCallbacks = new Dictionary<string, DesiredPropertyUpdateCallback>();

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private static readonly Dictionary<string, Dictionary<DateTimeOffset, double>> s_temperatureReadings = new Dictionary<string, Dictionary<DateTimeOffset, double>>();

        // Dictionary to hold the current temperature for each "Thermostat" component.
        private static readonly Dictionary<string, double> s_temperature = new Dictionary<string, double>();

        // Dictionary to hold the max temperature since last reboot, for each "Thermostat" component.
        private static readonly Dictionary<string, double> s_maxTemp = new Dictionary<string, double>();

        public static async Task Main(string[] _)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });
            s_logger = loggerFactory.CreateLogger<Program>();

            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Set handler to receive "reboot" command - root interface.
            // -> Set handler to receive "getMaxMinReport" command - on "Thermostat" components.
            // -> Set handler to receive "targetTemperature" property updates from service - on "Thermostat" components.
            // -> Send initial device info - "workingSet" over telemetry, "serialNumber" over reported property update - root interface.
            // -> Periodically send "temperature" over telemetry - on "Thermostat" components.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set - on "Thermostat" components.

            s_logger.LogDebug($"Initialize the device client.");
            InitializeDeviceClientAsync();

            s_logger.LogDebug($"Set handler for \"reboot\" command.");
            await s_deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, s_deviceClient);

            s_logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await s_deviceClient.SetMethodHandlerAsync("thermostat1*getMaxMinReport", HandleMaxMinReportCommandAsync, Thermostat1);
            await s_deviceClient.SetMethodHandlerAsync("thermostat2*getMaxMinReport", HandleMaxMinReportCommandAsync, Thermostat2);

            s_logger.LogDebug($"Set handler to receive \"targetTemperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(SetDesiredPropertyUpdateCallbackAsync, null);
            s_desiredPropertyUpdateCallbacks.Add(Thermostat1, TargetTemperatureUpdateCallbackAsync);
            s_desiredPropertyUpdateCallbacks.Add(Thermostat2, TargetTemperatureUpdateCallbackAsync);

            await SendDeviceMemoryAsync();
            await SendDeviceSerialNumberAsync();

            bool temperatureReset = true;
            s_maxTemp[Thermostat1] = 0d;
            s_maxTemp[Thermostat2] = 0d;

            await Task.Run(async () =>
            {
                while (true)
                {
                    if (temperatureReset)
                    {
                        // Generate a random value between 5.0°C and 45.0°C for the current temperature reading for each "Thermostat" component.
                        s_temperature[Thermostat1] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                        s_temperature[Thermostat2] = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                    }

                    await SendTemperatureAsync(Thermostat1);
                    await SendTemperatureAsync(Thermostat2);

                    temperatureReset = s_temperature[Thermostat1] == 0 && s_temperature[Thermostat2] == 0;
                    await Task.Delay(5 * 1000);
                }
            });
        }

        // Initialize the device client instance over Mqtt protocol (TCP, with fallback over Websocket), setting the ModelId into ClientOptions.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static void InitializeDeviceClientAsync()
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, TransportType.Mqtt, options);
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });
        }

        // Send working set of device memory over telemetry.
        private static async Task SendDeviceMemoryAsync()
        {
            string telemetryName = "workingSet";
            long workingSet = Environment.WorkingSet / 1024;

            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, JsonConvert.SerializeObject(workingSet));

            await s_deviceClient.SendEventAsync(msg);
            s_logger.LogDebug($"Telemetry: Sent - {{ \"{telemetryName}\": {workingSet}KiB }}.");
        }

        // Send device serial number over property update.
        private static async Task SendDeviceSerialNumberAsync()
        {
            string propertyName = "serialNumber";
            string propertyPatch = PnpHelper.CreatePropertyPatch(propertyName, JsonConvert.SerializeObject(SerialNumber));
            var reportedProperties = new TwinCollection(propertyPatch);

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            s_logger.LogDebug($"Property: Update - {{ \"{propertyName}\": \"{SerialNumber}\" }} is {StatusCode.Completed}.");
        }

        private static async Task SendTemperatureAsync(string componentName)
        {
            await SendTemperatureTelemetryAsync(componentName);

            double maxTemp = s_temperatureReadings[componentName].Values.Max<double>();
            if (maxTemp > s_maxTemp[componentName])
            {
                s_maxTemp[componentName] = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync(componentName);
            }
        }

        private static async Task SendTemperatureTelemetryAsync(string componentName)
        {
            string telemetryName = "temperature";
            double currentTemperature = s_temperature[componentName];
            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, JsonConvert.SerializeObject(currentTemperature), componentName);

            await s_deviceClient.SendEventAsync(msg);
            s_logger.LogDebug($"Telemetry: Sent - component=\"{componentName}\", {{ \"{telemetryName}\": {currentTemperature}°C }}.");

            if (s_temperatureReadings.ContainsKey(componentName))
            {
                s_temperatureReadings[componentName].TryAdd(DateTimeOffset.Now, currentTemperature);
            }
            else
            {
                s_temperatureReadings.TryAdd(componentName, new Dictionary<DateTimeOffset, double>() { { DateTimeOffset.Now, currentTemperature } });
            }
        }

        private static async Task UpdateMaxTemperatureSinceLastRebootAsync(string componentName)
        {
            string propertyName = "maxTempSinceLastReboot";
            double maxTemp = s_maxTemp[componentName];
            string propertyPatch = PnpHelper.CreatePropertyPatch(propertyName, JsonConvert.SerializeObject(maxTemp), componentName);
            var reportedProperties = new TwinCollection(propertyPatch);

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            s_logger.LogDebug($"Property: Update - component=\"{componentName}\", {{ \"{propertyName}\": {maxTemp}°C }} is {StatusCode.Completed}.");
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private static async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            int delay = JsonConvert.DeserializeObject<int>(request.DataAsJson);

            s_logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
            await Task.Delay(delay * 1000);

            s_temperature[Thermostat1] = s_maxTemp[Thermostat1] = 0;
            s_temperature[Thermostat2] = s_maxTemp[Thermostat2] = 0;

            s_temperatureReadings.Clear();

            return new MethodResponse((int)StatusCode.Completed);
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the specified time to the current time.
        private static async Task<MethodResponse> HandleMaxMinReportCommandAsync(MethodRequest request, object userContext)
        {
            string componentName = (string)userContext;
            DateTime since = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
            var sinceInDateTimeOffset = new DateTimeOffset(since);

            if (s_temperatureReadings.ContainsKey(componentName))
            {
                s_logger.LogDebug($"Command: Received - component=\"{componentName}\", generating max, min and avg temperature report since {sinceInDateTimeOffset.LocalDateTime}.");

                Dictionary<DateTimeOffset, double> allReadings = s_temperatureReadings[componentName];
                var filteredReadings = allReadings.Where(i => i.Key > sinceInDateTimeOffset).ToDictionary(i => i.Key, i => i.Value);

                if (filteredReadings != null && filteredReadings.Any())
                {
                    var report = new
                    {
                        maxTemp = filteredReadings.Values.Max<double>(),
                        minTemp = filteredReadings.Values.Min<double>(),
                        avgTemp = filteredReadings.Values.Average(),
                        startTime = filteredReadings.Keys.Min().DateTime,
                        endTime = filteredReadings.Keys.Max().DateTime,
                    };

                    s_logger.LogDebug($"Command: component=\"{componentName}\", MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                        $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime}, endTime={report.endTime}");

                    byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                    return await Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                }

                s_logger.LogDebug($"Command: component=\"{componentName}\", no relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, cannot generate any report.");
                return await Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
            }

            s_logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
            return await Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
        }

        private static async Task SetDesiredPropertyUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            bool callbackNotInvoked = true;

            foreach (KeyValuePair<string, object> propertyUpdate in desiredProperties)
            {
                string componentName = propertyUpdate.Key;
                if (s_desiredPropertyUpdateCallbacks.ContainsKey(componentName))
                {
                    s_desiredPropertyUpdateCallbacks[componentName]?.Invoke(desiredProperties, componentName);
                    callbackNotInvoked = false;
                }
            }

            if (callbackNotInvoked)
            {
                s_logger.LogDebug($"Property: Received a property update that is not implemented by any associated component.");
            }

            await Task.CompletedTask;
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and property update.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            string componentName = (string)userContext;
            string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = PnpHelper.GetPropertyFromTwin<double>(desiredProperties, propertyName, componentName);
            if (targetTempUpdateReceived)
            {
                s_logger.LogDebug($"Property: Received - component=\"{componentName}\", {{ \"{propertyName}\": {targetTemperature}°C }}.");

                string pendingPropertyPatch = PnpHelper.CreatePropertyEmbeddedValuePatch(
                    propertyName,
                    JsonConvert.SerializeObject(targetTemperature),
                    ackCode: (int)StatusCode.InProgress,
                    ackVersion: desiredProperties.Version,
                    componentName: componentName);

                var pendingReportedProperty = new TwinCollection(pendingPropertyPatch);
                await s_deviceClient.UpdateReportedPropertiesAsync(pendingReportedProperty);
                s_logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.InProgress}");

                // Update Temperature in 2 steps
                double step = (targetTemperature - s_temperature[componentName]) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    s_temperature[componentName] = Math.Round(s_temperature[componentName] + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string completedPropertyPatch = PnpHelper.CreatePropertyEmbeddedValuePatch(
                    propertyName,
                    JsonConvert.SerializeObject(s_temperature[componentName]),
                    ackCode: (int)StatusCode.Completed,
                    ackVersion: desiredProperties.Version,
                    serializedAckDescription: JsonConvert.SerializeObject("Successfully updated target temperature"),
                    componentName: componentName);

                var completedReportedProperty = new TwinCollection(completedPropertyPatch);
                await s_deviceClient.UpdateReportedPropertiesAsync(completedReportedProperty);
                s_logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {s_temperature[componentName]}°C }} is {StatusCode.Completed}");
            }
            else
            {
                s_logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.");
            }
        }
    }
}
