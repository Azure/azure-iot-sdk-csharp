// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//This code that messages to an IoT Hub for testing the routing as defined
//  in this article: https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing
//The scripts for creating the resources are included in the resources folder in this
//  Visual Studio solution.

using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArmReadWrite
{
    internal class Program
    {
        //This is the arm-read-write application that simulates a virtual device.
        //It writes messages to the IoT Hub, which routes the messages automatically to a storage account,
        //where you can view them.

        //  This was derived by the (more complicated) tutorial for routing
        //  https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing

        private static DeviceClient s_deviceClient;
        private static string s_myDeviceId;
        private static string s_iotHubUri;
        private static string s_deviceKey;

        private static async Task Main()
        {
            if (!ReadEnvironmentVariables())
            {
                Console.WriteLine();
                Console.WriteLine("Error! One or more environment variables not set");
                return;
            }

            Console.WriteLine("write messages to a hub and use routing to write them to storage");

            s_deviceClient = DeviceClient.Create(s_iotHubUri,
                new DeviceAuthenticationWithRegistrySymmetricKey(s_myDeviceId, s_deviceKey), TransportType.Mqtt);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            Task messages = SendDeviceToCloudMessagesAsync(cts.Token);

            Console.WriteLine($"Press Control+C at any time to quit the sample.");
            await messages;
        }

        /// <summary>
        /// Read local process environment variables for required values
        /// </summary>
        /// <returns>
        /// True if all required environment variables are set
        /// </returns>
        private static bool ReadEnvironmentVariables()
        {
            bool result = true;

            s_myDeviceId = Environment.GetEnvironmentVariable("IOT_DEVICE_ID");
            s_iotHubUri = Environment.GetEnvironmentVariable("IOT_HUB_URI");
            s_deviceKey = Environment.GetEnvironmentVariable("IOT_DEVICE_KEY");

            if (s_myDeviceId is null || s_iotHubUri is null || s_deviceKey is null)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken token)
        {
            double minTemperature = 20;
            double minHumidity = 60;
            var rand = new Random();

            while (!token.IsCancellationRequested)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                string infoString;
                string levelValue;

                if (rand.NextDouble() > 0.7)
                {
                    if (rand.NextDouble() > 0.5)
                    {
                        levelValue = "critical";
                        infoString = "This is a critical message.";
                    }
                    else
                    {
                        levelValue = "storage";
                        infoString = "This is a storage message.";
                    }
                }
                else
                {
                    levelValue = "normal";
                    infoString = "This is a normal message.";
                }

                var telemetryDataPoint = new
                {
                    deviceId = s_myDeviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    pointInfo = infoString
                };
                // serialize the telemetry data and convert it to JSON.
                string telemetryDataString = JsonConvert.SerializeObject(telemetryDataPoint);

                // Encode the serialized object using UTF-8 so it can be parsed by IoT Hub when
                // processing messaging rules.
                using var message = new Message(Encoding.UTF8.GetBytes(telemetryDataString))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                };

                //Add one property to the message.
                message.Properties.Add("level", levelValue);

                // Submit the message to the hub.
                await s_deviceClient.SendEventAsync(message, token);

                // Print out the message.
                Console.WriteLine("{0} > Sent message: {1}", DateTime.Now, telemetryDataString);

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}
