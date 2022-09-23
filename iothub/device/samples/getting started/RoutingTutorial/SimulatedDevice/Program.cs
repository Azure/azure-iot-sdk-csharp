// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    /// <summary>
    /// This is the code that sends messages to the IoT Hub for testing the routing as defined
    ///  in this article: https://docs.microsoft.com/azure/iot-hub/tutorial-routing
    /// The scripts for creating the resources are included in the resources folder in this
    ///  Visual Studio solution.
    ///
    /// This program encodes the message body so it can be queried against by the Iot hub.
    ///
    /// If you want to read an encoded message body, you can do this:
    ///   * route the message to storage,
    ///   * retrieve the message from the storage account by downloading the blob,
    ///   * use the ReadOneRowFromFile method at the bottom of this module to read the first row of the file.
    ///   * It will read the file, decode the first row in the file, and write it out to a new file
    ///       in ASCII so you can view it.
    /// </summary>
    internal class Program
    {
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

            // If this is false, it will submit messages to the iot hub.
            // If this is true, it will read one of the output files and convert it to ASCII.
            if (parameters.ReadTheFile)
            {
                // If you want to decode an output file, put the path in ReadOneRowFromFile(),
                //   uncomment the call here and the return command, then run this application.
                ReadOneRowFromFile(parameters.FilePath);
            }
            else
            {
                // Send messages to the simulated device. Each message will contain a randomly generated
                //   Temperature and Humidity.
                // The "level" of each message is set randomly to "storage", "critical", or "normal".
                // The messages are routed to different endpoints depending on the level, temperature, and humidity.
                //  This is set in the tutorial that goes with this sample:
                //  http://docs.microsoft.com/azure/iot-hub/tutorial-routing

                Console.WriteLine("Routing Tutorial: Simulated device\n");
                string myDeviceId = parameters.DeviceId;
                string deviceKey = parameters.DeviceKey;
                using var s_deviceClient = DeviceClient.Create(
                    parameters.IotHubHostName,
                    new DeviceAuthenticationWithRegistrySymmetricKey(myDeviceId, deviceKey),
                    TransportType.Mqtt);

                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("Sample execution cancellation requested; will exit.");
                };

                Task messages = SendDeviceToCloudMessagesAsync(myDeviceId, s_deviceClient, cts.Token);

                Console.WriteLine($"Press Control+C at any time to quit the sample.");
                await s_deviceClient.CloseAsync(cts.Token);
                await messages;
            }
        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async Task SendDeviceToCloudMessagesAsync(string myDeviceId, DeviceClient s_deviceClient, CancellationToken token)
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
                    deviceId = myDeviceId,
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

                // Add one property to the message.
                message.Properties.Add("level", levelValue);

                // Submit the message to the hub.
                try
                {
                    await s_deviceClient.SendEventAsync(message, token);
                }
                catch (OperationCanceledException) { }

                // Print out the message.
                Console.WriteLine("{0} > Sent message: {1}", DateTime.UtcNow, telemetryDataString);

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException) { }
            }
        }

        /// <summary>
        /// This method was written to enable you to decode one of the messages sent to the hub
        /// and view the body of the message.
        /// Route the messages to storage (they get written to blob storage).
        /// Send messages to the hub, by running this program with readthefile set to false.
        /// After some messages have been written to storage, download one of the files
        /// to somewhere you can find it, run the program
        /// with ReadThefile set to true and FilePath set to the file's location.
        /// This method will read in the output file, then convert the first line to a message body object
        /// and write it back out to a new file that you can open and view.
        /// </summary>
        private static void ReadOneRowFromFile(string filePath)
        {
            string filePathAndName = filePath + "47_utf32.txt";
            // Set the output file name.
            // Read in the file to an array of objects. These were encoded in Base64 when they were
            //   written.
            string outputFilePathAndName = filePathAndName.Replace(".txt", "_new.txt");
            string[] fileLines = File.ReadAllLines(filePathAndName);

            // Parse the first line into a message object. Retrieve the body as a string.
            //   This string was encoded as Base64 when it was written.
            JObject messageObject = JObject.Parse(fileLines[0]);
            string body = messageObject.Value<string>("Body");

            // Convert the body from Base64, then from UTF-32 to text, and write it out to the new file
            //   so you can view the result.
            string outputResult = Encoding.UTF32.GetString(System.Convert.FromBase64String(body));

            File.WriteAllText(outputFilePathAndName, outputResult);
        }
    }
}
