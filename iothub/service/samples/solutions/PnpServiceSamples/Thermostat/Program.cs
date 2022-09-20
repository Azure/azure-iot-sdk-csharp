// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// This sample performs root-level operations on a plug and play compatible device using the IoT hub service client
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
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

            ILogger logger = InitializeConsoleDebugLogger();
            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            logger.LogDebug("Set up the IoT hub service client and registry manager.");
            using var serviceClient = ServiceClient.CreateFromConnectionString(parameters.HubConnectionString);
            using var registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);

            logger.LogDebug("Set up and start the Thermostat service sample.");
            var thermostatSample = new ThermostatSample(serviceClient, registryManager, parameters.DeviceId, logger);
            await thermostatSample.RunSampleAsync();
        }

        private static ILogger InitializeConsoleDebugLogger()
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<ThermostatSample>();
        }
    }
}
