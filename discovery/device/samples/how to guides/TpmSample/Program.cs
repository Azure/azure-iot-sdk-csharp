// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Security;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Discovery.Client.Samples
{
    /// <summary>
    /// A sample to illustrate onboarding a device using the device provisioning service.
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

            // This sample provides a way to get the endorsement key (EK) required in creation of the individual enrollment
            if (parameters.GetTpmEndorsementKey)
            {
                using var security = new SecurityProviderTpmHsm(null);
                Console.WriteLine($"Your EK is {Convert.ToBase64String(security.GetEndorsementKey())}");
                Console.WriteLine($"Your SRK is {Convert.ToBase64String(security.GetStorageRootKey())}");

                return 0;
            }

            // For a normal run of this sample, IdScope and RegistrationId are required
            if (string.IsNullOrWhiteSpace(parameters.RegistrationId))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            var sample = new DiscoveryDeviceClientSample(parameters);
            await sample.RunSampleAsync();

            return 0;
        }
    }
}
