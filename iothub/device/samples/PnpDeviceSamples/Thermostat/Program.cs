// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using PnpHelpers;

namespace TemperatureController
{
    public class Program
    {
        private const string ModelId = "dtmi:com:example:TemperatureController;1";
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;

        public static async Task Main(string[] _)
        {
            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Send "current temperature" over both telemetry and property updates.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync();

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
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, TransportType.Mqtt, options);

            // Register a connection status change callback, that will get triggered any time the device's connection status changes.
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                PrintLog($"Connection status change registered - status={status}, reason={reason}");
            });

            // This will open the device client connection over Mqtt.
            await s_deviceClient.OpenAsync();
        }

        // Send the current temperature over telemetry and reported property.
        private static async Task SendCurrentTemperatureAsync()
        {
            // Send current temperature over telemetry.
            string telemetryName = "temperature";

            // Generate a random value between 40F and 90F for the current temperature reading.
            double currentTemperature = s_random.Next(40, 90);

            string telemetryPayload = PnpHelper.CreateTelemetryPayload(telemetryName, currentTemperature);
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            message.Properties.Add(PnpHelper.TelemetryComponentPropertyName, Thermostat1);

            // Alternate usage
            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, currentTemperature, Thermostat1);
            await s_deviceClient.SendEventAsync(msg);
            // end

            await s_deviceClient.SendEventAsync(message);
            PrintLog($"Sent current temperature {currentTemperature}F over telemetry.");

            // Send current temperature over reported property.
            string reportedPropertyPatch = PnpHelper.CreateReportedPropertiesPatch("currentTemperature", currentTemperature, Thermostat1);
            var reportedProperty = new TwinCollection(reportedPropertyPatch);
            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
            PrintLog($"Sent current temperature {currentTemperature}F over reported property update.");
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
