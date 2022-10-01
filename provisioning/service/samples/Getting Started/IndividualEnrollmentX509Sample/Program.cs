// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using IndividualEnrollmentX509Sample;

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

            using var certificate = new X509Certificate2(parameters.CertificatePath);

            using var provisioningServiceClient = new ProvisioningServiceClient(parameters.ProvisioningConnectionString);
            var sample = new IndividualEnrollmentX509Sample(provisioningServiceClient, certificate, parameters.DeviceId, parameters.RegistrationId);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
