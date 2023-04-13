// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class Program
    {
        /// <summary>
        /// A sample to manage an individual enrollment in device provisioning service with an X.509 certificate.
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

            // This sample accepts the provisioning connection string as a parameter, if present.
            Parameters.ValidateProvisioningConnectionString(parameters.ProvisioningConnectionString);

            // Set up logging
            using ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    // The SDK logs are written at Trace level. Set this to LogLevel.Trace to get ALL logs.
                    MinLogLevel = LogLevel.Debug,
                });
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            using var provisioningServiceClient = new ProvisioningServiceClient(parameters.ProvisioningConnectionString);
            var sample = new IndividualEnrollmentSample(
                provisioningServiceClient, 
                parameters.DeviceId, 
                parameters.RegistrationId, 
                logger);

            await sample.RunSampleAsync();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
