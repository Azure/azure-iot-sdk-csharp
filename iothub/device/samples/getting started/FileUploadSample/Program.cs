// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample requires an IoT Hub linked to a storage account container.
    /// Find instructions to configure a hub at <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-configure-file-upload"/>.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// A sample to illustrate how to upload files from a device.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        /// <returns></returns>
        public static async Task<int> Main(string[] args)
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

            // If both IoT hub and device connection strings exist, default behavior uses existing device
            if (parameters.DeviceConnectionString != null)
            {
                // run sample like normal
                using var deviceClient = DeviceClient.CreateFromConnectionString(
                    parameters.DeviceConnectionString,
                    parameters.TransportType);

                var sample = new FileUploadSample(deviceClient);
                await sample.RunSampleAsync();

                await deviceClient.CloseAsync();
            } 
            else if (parameters.HubConnectionString != null)
            {
                // Deploy new device to IoT hub
                using var registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);

                string deviceId = $"FileUploadSample_{Guid.NewGuid()}";
                var tempDevice = new Device(deviceId);

                await registryManager.AddDeviceAsync(tempDevice);
                using var deviceClient = DeviceClient.CreateFromConnectionString(
                    parameters.HubConnectionString,
                    deviceId,
                    parameters.TransportType);

                var sample = new FileUploadSample(deviceClient);
                await sample.RunSampleAsync();

                await deviceClient.CloseAsync();
            }
            else
            {
                throw new ArgumentNullException("Must specify either a device or IoT hub connection string.");
            }

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
