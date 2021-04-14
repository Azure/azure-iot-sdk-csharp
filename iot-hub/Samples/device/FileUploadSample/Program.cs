// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
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

            using var deviceClient = DeviceClient.CreateFromConnectionString(
                parameters.PrimaryConnectionString,
                parameters.TransportType);
            var sample = new FileUploadSample(deviceClient);
            await sample.RunSampleAsync();

            await deviceClient.CloseAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
