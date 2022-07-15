// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to manage enrollment groups in device provisioning service.
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

            if (string.IsNullOrWhiteSpace(parameters.ProvisioningConnectionString))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(parameters.ProvisioningConnectionString);
            var sample = new CleanupEnrollmentsSample(provisioningServiceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
