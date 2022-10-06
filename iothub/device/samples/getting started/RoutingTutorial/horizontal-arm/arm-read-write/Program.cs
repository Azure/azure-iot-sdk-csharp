﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This code that messages to an IoT Hub for testing the routing as defined
    ///  in this article: https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing
    /// The scripts for creating the resources are included in the resources folder in this
    ///  Visual Studio solution.
    /// </summary>
    internal class Program
    {
        //This is the arm-read-write application that simulates a virtual device.
        //It writes messages to the IoT Hub, which routes the messages automatically to a storage account,
        //where you can view them.

        //  This was derived by the (more complicated) tutorial for routing
        //  https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing

        private static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            var options = new IotHubClientOptions(parameters.GetHubTransportSettings());

            Console.WriteLine("write messages to a hub and use routing to write them to storage");
            using var deviceClient = new IotHubDeviceClient(
                parameters.PrimaryConnectionString,
                options);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            Console.WriteLine($"Press Control+C at any time to quit the sample.");

            try
            {
                await SendDeviceToCloudMessagesAsync(deviceClient, cts.Token);
            }
            catch (OperationCanceledException) { }
            await deviceClient.CloseAsync();

            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async Task SendDeviceToCloudMessagesAsync(IotHubDeviceClient deviceClient, CancellationToken token)
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
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    pointInfo = infoString
                };

                var message = new OutgoingMessage(telemetryDataPoint);

                //Add one property to the message.
                message.Properties.Add("level", levelValue);

                try
                {
                    await deviceClient.OpenAsync(token);
                    // Submit the message to the hub.
                    await deviceClient.SendEventAsync(message, token);

                    // Print out the message.
                    Console.WriteLine("{0} > Sent message: {1}", DateTime.Now, telemetryDataPoint);
                }
                catch (OperationCanceledException) { }

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}
