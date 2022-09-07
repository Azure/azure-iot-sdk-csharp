﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        private const string SdkEventProviderPrefix = "Microsoft-Azure-";

        /// <summary>
        /// A sample to illustrate how to recieve and complete file upload notifications.
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

            // Set up logging
            using ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    // The SDK logs are written at Trace level. Set this to LogLevel.Trace to get ALL logs.
                    MinLogLevel = LogLevel.Debug,
                });
            var logger = loggerFactory.CreateLogger<Program>();

            // Instantiating this seems to do all we need for outputting SDK events to our console log.
            using var sdkLogs = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            var runningTime = parameters.ApplicationRunningTime != null
                ? TimeSpan.FromSeconds((double)parameters.ApplicationRunningTime)
                : Timeout.InfiniteTimeSpan;

            var sample = new FileUploadNotificationReceiverSample(parameters.IoTHubConnectionString, parameters.TransportType, logger);
            await sample.RunSampleAsync(parameters.DeviceId, runningTime);

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
