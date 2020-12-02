// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Client.Samples;
using System;
using System.Threading.Tasks;

namespace SymmetricKeySample
{
    /// <summary>
    /// A sample to illustrate connecting a device to hub using the device provisioning service and a symmetric key.
    /// </summary>
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

            var sample = new ProvisioningDeviceClientSample(parameters);
            await sample.RunSampleAsync();

            Console.WriteLine("Enter any key to exit.");
            Console.ReadKey();

            return 0;
        }
    }
}
