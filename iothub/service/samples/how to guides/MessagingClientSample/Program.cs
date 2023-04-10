// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Client.Samples;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to demonstrate how to send cloud to device messages.
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
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            IotHubServiceClientOptions clientOptions = new IotHubServiceClientOptions()
            {
                RetryPolicy = new IotHubServiceExponentialBackoffRetryPolicy(uint.MaxValue, TimeSpan.MaxValue)
            };
            using var hubClient = new IotHubServiceClient(parameters.HubConnectionString, clientOptions);

            var sample = new MessagingClientSample(hubClient, parameters.DeviceId, logger);

            // Set up a way for the user to gracefully shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            await sample.SendMessagesAsync(cts.Token);

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
