// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate a device receiving methods.
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
                    if (parameters.Transport == Transport.Http1)
                    {
                        Console.WriteLine("Methods are not supported over HTTP.");
                        Environment.Exit(1);
                    }
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            var runningTime = parameters.ApplicationRunningTime != null
                ? TimeSpan.FromSeconds((double)parameters.ApplicationRunningTime)
                : Timeout.InfiniteTimeSpan;

            var options = new IotHubClientOptions(parameters.GetHubTransportSettings());
            using var deviceClient = new IotHubDeviceClient(
                parameters.PrimaryConnectionString,
                options);
            var sample = new MethodSample(deviceClient);
            await sample.RunSampleAsync(runningTime);
            await deviceClient.CloseAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
