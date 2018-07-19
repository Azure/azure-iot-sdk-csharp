// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        // The IoT Hub connection string. This is available under the "Shared access policies" in the Azure portal.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_CONN_STRING_CSHARP environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable

        private static string s_connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING_CSHARP");

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 0)
            {
                s_connectionString = args[0];
            }

            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(s_connectionString);

            var sample = new AutomaticDeviceManagementSample(registryManager);

            sample.RunSampleAsync().GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
