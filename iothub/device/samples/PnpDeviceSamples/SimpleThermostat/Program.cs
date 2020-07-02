// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace SimpleThermostat
{
    public class Program
    {
        private const string ModelId = "dtmi:com:example:simplethermostat;1";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;

        private static double s_temperature = 0d;

        public static async Task Main(string[] _)
        {
            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "reboot" command, and reset the "Temperature" over telemetry and reported property.
            // -> Periodically send "currentTemperature" over both telemetry and property updates.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync();

            PrintLog($"Set handler to receive \"target temperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient);

            PrintLog($"Set handler for \"reboot\" command");
            await s_deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, s_deviceClient);

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

                    // Send the current temperature over telemetry and reported property.
                    await SendTemperatureTelemetryAsync();
                    await SendCurrentTemperaturePropertyAsync();

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
            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, "targetTemperature");
            if (targetTempUpdateReceived)
            {
                PrintLog($"Received an update for target temperature {targetTemperature}°C");

                // Increment Temperature in 2 steps
                double step = (targetTemperature - s_temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    s_temperature += step;
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"targetTemperature\": {{ \"value\": {s_temperature}, \"ac\": 200, \"av\": {desiredProperties.Version} }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                PrintLog($"Processed an update for target temperature {s_temperature}°C over reported property.");
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
        }

        private static async Task SendCurrentTemperaturePropertyAsync()
        {
            var reportedProperty = new TwinCollection();
            reportedProperty["currentTemperature"] = s_temperature;
            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
            PrintLog($"Sent current temperature {s_temperature}°C over reported property update.");
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry,
        // and also reset the temperature property to 0.
        private static async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            int delay = JObject.Parse(request.DataAsJson).Value<int>("delay");

            PrintLog($"Rebooting thermostat: resetting current temperature reading to 0°C after {delay} seconds");
            await Task.Delay(delay * 1000);
            s_temperature = 0d;

            return new MethodResponse(200);
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
