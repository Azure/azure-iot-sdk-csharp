// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples

using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry and receiving a command.
    /// For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples/how%20to%20guides/DeviceReconnectionSample"/>.
    /// </summary>
    /// <param name="args">
    /// Run with `--help` to see a list of required and optional parameters.
    /// </param>
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams => parameters = parsedParams)
                .WithNotParsed(errors => Environment.Exit(1));

            TimeSpan? appRunTime = null;

            if (parameters.ApplicationRunningTime.HasValue)
            {
                Console.WriteLine($"Running sample for a max time of {parameters.ApplicationRunningTime.Value} seconds.");
                appRunTime = TimeSpan.FromSeconds(parameters.ApplicationRunningTime.Value);
            }

            Console.WriteLine("IoT Hub Quickstarts - Simulated device with command.");
            var options = new IotHubClientOptions(parameters.GetHubTransportSettings());

            // Connect to the IoT hub using the MQTT protocol by default
            using var deviceClient = new IotHubDeviceClient(parameters.DeviceConnectionString, options);
            var sample = new SimulatedDeviceWithCommand(deviceClient, appRunTime);
            await sample.RunSampleAsync();
            await deviceClient.CloseAsync();

            Console.WriteLine("Done.\n");
        }
    }
}
