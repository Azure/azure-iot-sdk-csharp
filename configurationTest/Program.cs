// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
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

            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);

            var sample = new EdgeDeploymentSample(registryManager);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
        }
    }
}
