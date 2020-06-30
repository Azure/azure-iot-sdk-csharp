// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace SimpleThermostat
{
    public class Program
    {
        private const string DeviceConnectionString = "device_connection_string_here";
        private const string ModelId = "dtmi:com:example:simplethermostat;1";

        private static DeviceClient s_deviceClient;

        public static async Task Main(string[] _)
        {
            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Set handler to receive "target temperature" updates.
            // -> Set handler to receive "reboot" command.
            // -> Retrieve current "target temperature".
            // -> Send "current temperature" over both telemetry and property updates.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync();

            PrintLog($"Set handler to receive \"target temperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient);

            PrintLog($"Set handler for \"reboot\" command");
            await s_deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, s_deviceClient);

            PrintLog($"Send current temperature reading.");
            await SendCurrentTemperatureAsync();

            PrintLog($"Press any key to exit");
            Console.ReadKey();
        }

        // Initialize the device client instance over Mqtt protocol, setting the ModelId into ClientOptions, and open the connection.
        // This method also sets a connection status change callback.
        private static async Task InitializeDeviceClientAsync()
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            // Initialize the device client instance using the device connection string, transport of Mqtt over TCP (with fallback to Websocket),
            // and the device ModelId set in ClientOptions.
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt, options);

            // Register a connection status change callback, that will get triggered any time the device's connection status changes.
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                PrintLog($"Connection status change registered - status={status}, reason={reason}");
            });

            // This will open the device client connection over Mqtt.
            await s_deviceClient.OpenAsync();
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and reported property update.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, "targetTemperature");
            if (targetTempUpdateReceived)
            {
                PrintLog($"Received an update for target temperature");
                await UpdateCurrentTemperatureAsync(targetTemperature);
            }
            else
            {
                PrintLog($"Received an unrecognized property update from service");
            }
        }

        // Send the current temperature over telemetry and reported property.
        private static async Task SendCurrentTemperatureAsync()
        {
            // Send current temperature over telemetry.
            string telemetryName = "temperature";

            // Generate a random value between 40F and 90F for the current temperature reading.
            double currentTemperature = new Random().Next(40, 90);

            string telemetryPayload = $"{{ \"{telemetryName}\": {currentTemperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message);
            PrintLog($"Sent current temperature {currentTemperature}F over telemetry.");

            // Send current temperature over reported property.
            var reportedProperty = new TwinCollection();
            reportedProperty["currentTemperature"] = currentTemperature;
            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
            PrintLog($"Sent current temperature {currentTemperature}F over reported property update.");
        }

        // Update the temperature over telemetry and reported property, based on the target temperature update received.
        private static async Task UpdateCurrentTemperatureAsync(double targetTemperature)
        {
            // Send temperature update over telemetry.
            string telemetryName = "temperature";
            string telemetryPayload = $"{{ \"{telemetryName}\": {targetTemperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message);
            PrintLog($"Sent current temperature {targetTemperature}F over telemetry.");

            // Send temperature update over reported property.
            var reportedProperty = new TwinCollection();
            reportedProperty["currentTemperature"] = targetTemperature;
            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
            PrintLog($"Sent current temperature {targetTemperature}F over reported property update.");
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0) over telemetry,
        // and also reset the temperature property to 0.
        private static async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            PrintLog("Rebooting thermostat: resetting current temperature reading to 0.0");
            await UpdateCurrentTemperatureAsync(0);

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
