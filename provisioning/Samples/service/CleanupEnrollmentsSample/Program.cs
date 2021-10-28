// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

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

        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0)
            {
                s_connectionString = args[0];
            }

            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(s_connectionString);
            var sample = new CleanupEnrollmentsSample(provisioningServiceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
