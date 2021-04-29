// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate how to send telemetry messages.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
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

            TimeSpan? appRunTime = null;
            if (parameters.ApplicationRunningTime.HasValue)
            {
                Console.WriteLine($"Running sample for a max time of {parameters.ApplicationRunningTime.Value} seconds.");
                appRunTime = TimeSpan.FromSeconds(parameters.ApplicationRunningTime.Value);
            }

            using var deviceClient = DeviceClient.CreateFromConnectionString(
                parameters.PrimaryConnectionString,
                parameters.TransportType);
            var sample = new MessageReceiveSample(deviceClient, appRunTime);
            await sample.RunSampleAsync();
            await deviceClient.CloseAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
