// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub service SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/service

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples.InvokeDeviceMethod
{
    /// <summary>
    /// This sample illustrates the very basics of a service app invoking a method on a device.
    /// </summary>
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts - InvokeDeviceMethod application.");

            // Parse sample parameters.
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams => parameters = parsedParams)
                .WithNotParsed(errors => Environment.Exit(1));

            Debug.Assert(!string.IsNullOrWhiteSpace(parameters.HubConnectionString),
                    "An IoT hub connection string needs to be specified, " +
                    "please set the environment variable \"IOTHUB_DEVICE_CONNECTION_STRING\" " +
                    "or pass in \"-c | --DeviceConnectionString\" through command line.");

            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            using var serviceClient = new IotHubServiceClient(parameters.HubConnectionString);

            await InvokeMethodAsync(serviceClient, parameters.DeviceId);

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        // Invoke the direct method on the device, passing the payload.
        private static async Task InvokeMethodAsync(IotHubServiceClient serviceClient, string deviceId)
        {
            var methodInvocation = new DirectMethodServiceRequest
            {
                MethodName = "SetTelemetryInterval",
                ResponseTimeout = TimeSpan.FromSeconds(30),
                Payload = "10",
            };

            Console.WriteLine($"Invoking direct method for device: {deviceId}");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            DirectMethodClientResponse response = await serviceClient.DirectMethods.InvokeAsync(deviceId, methodInvocation);

            Console.WriteLine($"Response status: {response.Status}, payload:\n\t{response.PayloadAsString}");
        }
    }
}
