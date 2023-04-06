// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Client.Samples;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate bulk devices deletion.
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

            IotHubServiceClientOptions clientOptions = new IotHubServiceClientOptions()
            {
                RetryPolicy = new IotHubServiceExponentialBackoffRetryPolicy(uint.MaxValue, TimeSpan.MaxValue)
            };
            using var hubClient = new IotHubServiceClient(parameters.HubConnectionString, clientOptions);

            var sample = new MessagingClientSample(hubClient, parameters.DeviceId);

            await sample.SendMessagesAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
