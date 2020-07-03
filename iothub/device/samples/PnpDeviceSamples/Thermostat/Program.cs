// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thermostat
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
    }

    public class Program
    {
        private const string ModelId = "dtmi:com:example:Thermostat;1";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;

        private static double s_temperature = 0d;

        // Dictionary to hold the temperature updates sent over.
        private static readonly Dictionary<DateTimeOffset, double> s_temperatureReadings = new Dictionary<DateTimeOffset, double>();

        public static async Task Main(string[] _)
        {
            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Periodically send "temperature" over telemetry and "max temperature since last reboot" over property update.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync();

            PrintLog($"Set handler to receive \"target temperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient);

            PrintLog($"Set handler for \"getMaxMinReport\" command");
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
                    }

                    await SendTemperatureTelemetryAsync();
                    await SendMaxTemperatureSinceLastRebootAsync();

                    temperatureReset = s_temperature == 0;
                    await Task.Delay(5 * 1000);
                }
            });
        }

        // Initialize the device client instance over Mqtt protocol (TCP, with fallback over Websocket), setting the ModelId into ClientOptions, and open the connection.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static async Task InitializeDeviceClientAsync()
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, TransportType.Mqtt, options);
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                PrintLog($"Connection status change registered - status={status}, reason={reason}");
            });

            await s_deviceClient.OpenAsync();
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and reported property update.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, propertyName);
            if (targetTempUpdateReceived)
            {
                PrintLog($"Received an update for target temperature {targetTemperature}°C");

                string jsonPropertyPending = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int)StatusCode.InProgress}, \"av\": {desiredProperties.Version} }} }}";
                var reportedPropertyPending = new TwinCollection(jsonPropertyPending);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedPropertyPending);
                PrintLog($"Property update for {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.InProgress}");

                // Increment Temperature in 2 steps
                double step = (targetTemperature - s_temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    s_temperature = Math.Round(s_temperature + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int) StatusCode.Completed}, \"av\": {desiredProperties.Version} }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                PrintLog($"Property update for {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.Completed}");
            }
            else
            {
                PrintLog($"Received an unrecognized property update from service");
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
            PrintLog($"Sent current temperature {s_temperature}°C over telemetry.");

            s_temperatureReadings.Add(DateTimeOffset.Now, s_temperature);
        }

        private static async Task SendMaxTemperatureSinceLastRebootAsync()
        {
            string propertyName = "maxTempSinceLastReboot";
            double maxTemp = s_temperatureReadings.Values.Max<double>();

            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = maxTemp;

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            PrintLog($"Sent max temperature since last reboot {maxTemp}°C over property update.");
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the specified time to the current time.
        private static async Task<MethodResponse> HandleMaxMinReportCommandAsync(MethodRequest request, object userContext)
        {
            DateTimeOffset since = JObject.Parse(request.DataAsJson).Value<DateTime>("since");
            PrintLog($"Generating min, max, avg temperature report since {since}.");

            var filteredReadings = s_temperatureReadings.Where(i => i.Key > since).ToDictionary(i => i.Key, i => i.Value);

            var report = new
            {
                maxTemp = filteredReadings.Values.Max<double>(),
                minTemp = filteredReadings.Values.Min<double>(),
                avgTemp = filteredReadings.Values.Average(),
                startTime = filteredReadings.Keys.Min().DateTime.ToUniversalTime(),
                endTime = filteredReadings.Keys.Max().DateTime.ToUniversalTime(),
            };

            PrintLog($"MinMaxReport since {since}:" +
                $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime}, endTime={report.endTime}");

            byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
            return await Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
        }

        private static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName)
        {
            return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
        }

        private static void PrintLog(string message)
        {
            Console.WriteLine($">> {DateTime.Now}: {message}");
        }
    }
}
