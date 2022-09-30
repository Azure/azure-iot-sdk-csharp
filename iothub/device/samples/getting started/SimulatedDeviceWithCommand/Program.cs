// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace SimulatedDeviceWithCommand
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry and receiving a command.
    /// For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    internal class Program
    {
        private static DeviceClient s_deviceClient;
        private static TimeSpan s_telemetryInterval = TimeSpan.FromSeconds(1); // Seconds

        private static async Task Main(string[] args)
        {
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams => parameters = parsedParams)
                .WithNotParsed(errors => Environment.Exit(1));

            Console.WriteLine("IoT Hub Quickstarts - Simulated device with command.");

            // Connect to the IoT hub using the MQTT protocol by default
            s_deviceClient = DeviceClient.CreateFromConnectionString(parameters.DeviceConnectionString, parameters.TransportType);

            // Create a handler for the direct method call
            await s_deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);

            // Set up a condition to quit the sample
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the telemetry loop
            await SendDeviceToCloudMessagesAsync(cts.Token);

            // SendDeviceToCloudMessagesAsync is designed to run until cancellation has been explicitly requested by Console.CancelKeyPress.
            // As a result, by the time the control reaches the call to close the device client, the cancellation token source would
            // have already had cancellation requested.
            // Hence, if you want to pass a cancellation token to any subsequent calls, a new token needs to be generated.
            // For device client APIs, you can also call them without a cancellation token, which will set a default
            // cancellation timeout of 4 minutes: https://github.com/Azure/azure-iot-sdk-csharp/blob/64f6e9f24371bc40ab3ec7a8b8accbfb537f0fe1/iothub/device/src/InternalClient.cs#L1922
            await s_deviceClient.CloseAsync();

            s_deviceClient.Dispose();
            Console.WriteLine("Device simulator finished.");
        }

        // Handle the direct method call.
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            string data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value.
            if (int.TryParse(data, out int telemetryIntervalInSeconds))
            {
                s_telemetryInterval = TimeSpan.FromSeconds(telemetryIntervalInSeconds);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Telemetry interval set to {s_telemetryInterval}");
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message.
                string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message.
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        // Async method to send simulated telemetry.
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            // Initial telemetry values.
            double minTemperature = 20;
            double minHumidity = 60;
            var rand = new Random();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double currentTemperature = minTemperature + rand.NextDouble() * 15;
                    double currentHumidity = minHumidity + rand.NextDouble() * 20;

                    // Create JSON message.
                    string messageBody = JsonSerializer.Serialize(
                        new
                        {
                            temperature = currentTemperature,
                            humidity = currentHumidity,
                        });
                    using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                    {
                        ContentType = "application/json",
                        ContentEncoding = "utf-8",
                    };

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                    // Send the telemetry message.
                    await s_deviceClient.SendEventAsync(message, ct);
                    Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

                    await Task.Delay(s_telemetryInterval, ct);
                }
            }
            catch (TaskCanceledException) { } // User canceled
        }
    }
}
