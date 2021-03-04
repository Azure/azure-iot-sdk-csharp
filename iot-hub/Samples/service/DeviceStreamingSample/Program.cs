// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using ServiceClientStreamingSample;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            using ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(parameters.HubConnectionString, parameters.TransportType);
            var sample = new DeviceStreamSample(serviceClient, parameters.DeviceId);
            await sample.RunSampleAsync().ConfigureAwait(false);

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
