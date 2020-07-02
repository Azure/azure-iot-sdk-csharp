// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnpHelpers;

namespace TemperatureController
{
    public class Program
    {
        private const string ModelId = "dtmi:com:example:TemperatureController;1";
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";
        private const string SerialNumber = "SR-123456";

        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;

        // A dictionary to hold all desired property change callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the componentName.
        // TODO - Implementation for a root property.
        private static ConcurrentDictionary<string, DesiredPropertyUpdateCallback> s_desiredPropertyUpdateCallbacks = new ConcurrentDictionary<string, DesiredPropertyUpdateCallback>();

        public static async Task Main(string[] _)
        {
            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Following root interface implementations -
            //      -> Send "working set of device memory" over telemetry.
            //      -> Send "device serial no" over property update.
            //      -> Set handler to receive "reboot" command.
            // -> Following Thermostat interface implementations -
            //      -> Send "current temperature" over telemetry.
            //      -> Set handler to receive "target temperature" property update.
            //      -> Send "max temperature since last reboot" over property update.
            //      -> Set handler to receive "getMaxMinReport" command.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync();

            PrintLog($"Send working set of device memory.");
            await SendDeviceMemoryAsync();

            PrintLog($"Send device serial no.");
            await SendDeviceSerialNumberAsync();

            PrintLog($"Set handler for \"reboot\" command");
            await s_deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, s_deviceClient);

            PrintLog($"Send current temperature reading.");
            await SendCurrentTemperatureAsync(Thermostat1);
            await SendCurrentTemperatureAsync(Thermostat2);

            PrintLog($"Set handler to receive \"target temperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(SetDesiredPropertyUpdateCallbackAsync, null);
            s_desiredPropertyUpdateCallbacks.TryAdd(Thermostat1, TargetTemperatureUpdateCallbackAsync);
            s_desiredPropertyUpdateCallbacks.TryAdd(Thermostat2, TargetTemperatureUpdateCallbackAsync);

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

        // Send working set of device memory over telemetry.
        private static async Task SendDeviceMemoryAsync()
        {
            string telemetryName = "workingSet";
            long workingSet = Environment.WorkingSet / 1024;

            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, JsonConvert.SerializeObject(workingSet));

            await s_deviceClient.SendEventAsync(msg);
            PrintLog($"Sent working set availability over telemetry - {workingSet}KiB.");
        }

        // Send device serial number over property update.
        private static async Task SendDeviceSerialNumberAsync()
        {
            string propertyName = "serialNumber";
            string propertyPatch = PnpHelper.CreateReadonlyReportedPropertiesPatch(propertyName, JsonConvert.SerializeObject(SerialNumber));
            var reportedProperties = new TwinCollection(propertyPatch);

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            PrintLog($"Sent device serial number \"{SerialNumber}\" over property update.");
        }

        // Send the current temperature over telemetry.
        private static async Task SendCurrentTemperatureAsync(string componentName)
        {
            // Send current temperature over telemetry.
            string telemetryName = "temperature";

            // Generate a random value between 10°C and 30°C for the current temperature reading.
            double currentTemperature = s_random.Next(10, 30);
            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, JsonConvert.SerializeObject(currentTemperature), componentName);

            await s_deviceClient.SendEventAsync(msg);
            PrintLog($"Sent current temperature {currentTemperature}°C for component {componentName} over telemetry.");
        }

        // Update the temperature over telemetry, based on the target temperature update or "reboot" command received.
        private static async Task UpdateCurrentTemperatureAsync(double targetTemperature, string componentName)
        {
            // Send temperature update over telemetry.
            string telemetryName = "temperature";
            Message msg = PnpHelper.CreateIothubMessageUtf8(telemetryName, JsonConvert.SerializeObject(targetTemperature), componentName);

            await s_deviceClient.SendEventAsync(msg);
            PrintLog($"Sent current temperature {targetTemperature}°C for component {componentName} over telemetry.");
        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private static async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            int delay = JObject.Parse(request.DataAsJson).Value<int>("delay");

            PrintLog($"Rebooting thermostat: resetting current temperature reading to 0°C after {delay} seconds");
            await Task.Delay(delay * 1000);
            await UpdateCurrentTemperatureAsync(0, Thermostat1);
            await UpdateCurrentTemperatureAsync(0, Thermostat2);

            return new MethodResponse(200);
        }

        private static async Task SetDesiredPropertyUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            bool callbackNotInvoked = true;

            foreach(KeyValuePair<string, object> propertyUpdate in desiredProperties)
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
                PrintLog($"Received a property update that is not implemented by any associated component.");
            }

            await Task.CompletedTask;
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            string componentName = (string)userContext;
            string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = PnpHelper.GetPropertyFromTwin<double>(desiredProperties, propertyName, componentName);
            if (targetTempUpdateReceived)
            {
                PrintLog($"Received an update for target temperature of {targetTemperature}°C for component {componentName}");
                await UpdateCurrentTemperatureAsync(targetTemperature, componentName);

                string reportedPropertyPatch = PnpHelper.CreateWriteableReportedPropertyPatch(
                    propertyName,
                    JsonConvert.SerializeObject(targetTemperature),
                    ackCode: 200,
                    ackVersion: 1,
                    componentName: componentName);

                var reportedProperty = new TwinCollection(reportedPropertyPatch);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                PrintLog($"Sent target temperature of {targetTemperature}°C for component {componentName} over property update.");
            }

            // TODO: targetTempUpdateReceived value needs to be relayed to SetDesiredPropertyUpdateCallbackAsync as well.
        }

        private static void PrintLog(string message)
        {
            Console.WriteLine($">> {DateTime.Now}: {message}");
        }
    }
}
