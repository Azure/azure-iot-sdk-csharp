// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// A sample to illustrate provisioning a device using DPS with a Certificate Signing Request (CSR).
    /// The device receives an issued certificate from DPS which can then be used to authenticate with IoT Hub.
    /// </summary>
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Parse application parameters
            Parameters? parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Failed to parse command line arguments.");
                });

            if (parameters == null)
            {
                return 1;
            }

            // Validate parameters
            if (parameters.AuthType == AuthenticationType.SymmetricKey && string.IsNullOrEmpty(parameters.SymmetricKey))
            {
                Console.WriteLine("Error: SymmetricKey (-k) is required when using SymmetricKey authentication.");
                return 1;
            }

            if (parameters.AuthType == AuthenticationType.X509 && string.IsNullOrEmpty(parameters.X509CertPath))
            {
                Console.WriteLine("Error: X509CertPath (-c) is required when using X509 authentication.");
                return 1;
            }

            try
            {
                var sample = new ProvisioningDeviceClientCsrSample(parameters);
                await sample.RunSampleAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}
