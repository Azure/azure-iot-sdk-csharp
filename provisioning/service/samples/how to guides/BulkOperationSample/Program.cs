// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using BulkOperationSample;
using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;

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

            // Set up logging
            using ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    // The SDK logs are written at Trace level. Set this to LogLevel.Trace to get ALL logs.
                    MinLogLevel = LogLevel.Debug,
                });
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            if (string.IsNullOrWhiteSpace(parameters.ProvisioningConnectionString))
            {
                logger.LogError(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using var provisioningServiceClient = new ProvisioningServiceClient(parameters.ProvisioningConnectionString);
            var sample = new BulkOperationSample(provisioningServiceClient, logger);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
