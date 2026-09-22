// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class Program
    {
        public static async Task Main(string[] args)
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

            // Select the service API version. The default (2019-03-31) preserves legacy wire behavior;
            // 2026-11-01 and 2026-11-02-preview are explicit opt-in versions (the preview additionally
            // enables preview-only fields).
            var options = new ProvisioningServiceClientOptions
            {
                Version = parameters.GetServiceVersion(),
            };

            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(parameters.ProvisioningConnectionString, options);
            var sample = new IndividualEnrollmentSample(provisioningServiceClient, parameters);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.\n");
        }
    }
}
