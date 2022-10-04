// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;
using IndividualEnrollmentTPMSample;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
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

            if (string.IsNullOrWhiteSpace(parameters.ProvisioningConnectionString))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using var provisioningServiceClient = new ProvisioningServiceClient(parameters.ProvisioningConnectionString);
            var sample = new IndividualEnrollmentTPMSample(provisioningServiceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
