// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class Program
    {
        // The Provisioning Service connection string. This is available under the "Shared access policies" in the Azure portal.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the PROVISIONING_CONNECTION_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_connectionString = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("EnrollmentGroupSample <groupIssuerCertificate.cer>");
                return 1;
            }

            X509Certificate2 certificate = new X509Certificate2(args[0]);

            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 1)
            {
                s_connectionString = args[1];
            }
           
            using (var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(s_connectionString))
            {
                var sample = new EnrollmentGroupSample(provisioningServiceClient, certificate);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
