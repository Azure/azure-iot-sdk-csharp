// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// A sample to illustrate using registry manager.
    /// </summary>
    /// <param name="args">
    /// Run with `--help` to see a list of required and optional parameters.
    /// </param>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(
                    parsedParams =>
                    {
                        parameters = parsedParams;
                    })
                .WithNotParsed(
                    errors =>
                    {
                        Environment.Exit(1);
                    });

            var sample = new RegistryManagerSample(parameters);
            await sample.RunSampleAsync();

            Console.WriteLine("The sample has completed.");
        }
    }
}
