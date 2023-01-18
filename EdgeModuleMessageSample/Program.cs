// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        /// <summary>
        /// This sample demonstrates how to send and receive messages on an Azure edge module.
        /// </summary>
        /// ///<remarks>
        /// For simplicity, this sample sends telemetry messages to the module itself.
        /// </remarks>
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
            string outputTarget = null;
            if (parameters.ApplicationRunningTime.HasValue)
            {
                Console.WriteLine($"Running sample for a max time of {parameters.ApplicationRunningTime.Value} seconds.");
                appRunTime = TimeSpan.FromSeconds(parameters.ApplicationRunningTime.Value);
                outputTarget = parameters.OutputTarget;
            }

            var options = new IotHubClientOptions(parameters.GetHubTransportSettings());
            await using var moduleClient = new IotHubModuleClient(
                parameters.PrimaryConnectionString,
                options);
            var sample = new EdgeModuleMessageSample(moduleClient, outputTarget, appRunTime);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}