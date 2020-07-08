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
using Newtonsoft.Json.Linq;

namespace Thermostat
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404
    }

    public class Program
    {
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json
        private const string ModelId = "dtmi:com:example:Thermostat;1";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;
        private static ILogger s_logger;

        private static double s_temperature = 0d;
        private static double s_maxTemp = 0d;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private static readonly Dictionary<DateTimeOffset, double> s_temperatureReadings = new Dictionary<DateTimeOffset, double>();

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
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            s_logger.LogDebug($"Initialize the device client.");
            InitializeDeviceClientAsync();

            s_logger.LogDebug($"Set handler to receive \"targetTemperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient);

            s_logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await s_deviceClient.SetMethodHandlerAsync("getMaxMinReport", HandleMaxMinReportCommandAsync, s_deviceClient);

            bool temperatureReset = true;
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (temperatureReset)
                    {
                        // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                        s_temperature = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                        temperatureReset = false;
                    }

                    await SendTemperatureAsync();
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

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and reported property update.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, propertyName);
            if (targetTempUpdateReceived)
            {
                s_logger.LogDebug($"Property: Received - {{ \"{propertyName}\": {targetTemperature}°C }}.");

                string jsonPropertyPending = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int)StatusCode.InProgress}, \"av\": {desiredProperties.Version} }} }}";
                var reportedPropertyPending = new TwinCollection(jsonPropertyPending);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedPropertyPending);
                s_logger.LogDebug($"Property: Update - {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.InProgress}.");

                // Update Temperature in 2 steps
                double step = (targetTemperature - s_temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    s_temperature = Math.Round(s_temperature + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int)StatusCode.Completed}, \"av\": {desiredProperties.Version}, \"ad\": \"Successfully updated target temperature\" }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                s_logger.LogDebug($"Property: Update - {{\"{propertyName}\": {s_temperature}°C }} is {StatusCode.Completed}.");
            }
            else
            {
                s_logger.LogDebug($"Property: Received an unrecognized property update from service.");
            }
        }

        private static async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = s_temperatureReadings.Values.Max<double>();
            if (maxTemp > s_maxTemp)
            {
                s_maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync();
            }
        }

        private static async Task SendTemperatureTelemetryAsync()
        {
            string telemetryName = "temperature";

            string telemetryPayload = $"{{ \"{telemetryName}\": {s_temperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message);
            s_logger.LogDebug($"Telemetry: Sent - {{ \"{telemetryName}\": {s_temperature}°C }}.");

            s_temperatureReadings.Add(DateTimeOffset.Now, s_temperature);
        }

        private static async Task UpdateMaxTemperatureSinceLastRebootAsync()
        {
            string propertyName = "maxTempSinceLastReboot";

            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = s_maxTemp;

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            s_logger.LogDebug($"Property: Update - {{ \"{propertyName}\": {s_maxTemp}°C }} is {StatusCode.Completed}.");
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the specified time to the current time.
        private static async Task<MethodResponse> HandleMaxMinReportCommandAsync(MethodRequest request, object userContext)
        {
            DateTime since = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
            var sinceInDateTimeOffset = new DateTimeOffset(since);
            s_logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since {sinceInDateTimeOffset.LocalDateTime}.");

            var filteredReadings = s_temperatureReadings.Where(i => i.Key > sinceInDateTimeOffset).ToDictionary(i => i.Key, i => i.Value);

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

                s_logger.LogDebug($"Command: MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                    $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime}, endTime={report.endTime}");

                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                return await Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
            }

            s_logger.LogDebug($"Command: No relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, cannot generate any report.");
            return await Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
        }

        private static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName)
        {
            return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
        }
    }
}
