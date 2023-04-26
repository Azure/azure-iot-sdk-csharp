﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Azure.Devices.Provisioning.Client.Samples;
using Microsoft.Extensions.Logging;

namespace SymmetricKeySample
{
    /// <summary>
    /// A sample to illustrate connecting a device to hub using the device provisioning service and a symmetric key.
    /// </summary>
    internal class Program
    {
        private const string SdkEventProviderPrefix = "Microsoft-Azure-";

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

            // Set up logging
            using ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    // The SDK logs are written at Trace level. Set this to LogLevel.Trace to get ALL logs.
                    MinLogLevel = LogLevel.Debug,
                });
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            // Instantiating this seems to do all we need for outputting SDK events to our console log.
            using var sdkLogs = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            var sample = new ProvisioningDeviceClientSample(parameters, logger);
            await sample.RunSampleAsync();
        }
    }
}
