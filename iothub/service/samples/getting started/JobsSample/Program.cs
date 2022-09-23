// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples.JobsSample
{
    internal class Program
    {
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

            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using var jobClient = new IotHubServiceClient(parameters.HubConnectionString);

            var sample = new JobsSample(jobClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
