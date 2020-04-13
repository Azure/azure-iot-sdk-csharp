// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private static string s_pathToDevicePrefixForDeletionFile = Environment.GetEnvironmentVariable("PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE");

        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0)
            {
                s_connectionString = args[0];
            }

            if (string.IsNullOrEmpty(s_pathToDevicePrefixForDeletionFile))
            {
                if (args.Length > 1)
                {
                    s_pathToDevicePrefixForDeletionFile = args[1];
                } 
                else
                {
                    Console.WriteLine("Please provide the absolute path to csv file containing prefixes of devices to be deleted, exiting...");
                    return 1;
                }
            }

            using RegistryManager rm = RegistryManager.CreateFromConnectionString(s_connectionString);
            var deleteDeviceWithPrefix = ConvertCsvFileToList(s_pathToDevicePrefixForDeletionFile);

            var sample = new CleanUpDevicesSample(rm, deleteDeviceWithPrefix);
            await sample.RunCleanUpAsync().ConfigureAwait(false);

            Console.WriteLine("Done.");
            return 0;
        }

        private static List<string> ConvertCsvFileToList(string filePath)
        {
            List<string> deleteDeviceWithPrefix = new List<string>();
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                string[] words = line.Split(',');
                deleteDeviceWithPrefix.AddRange(words);
            }

            return deleteDeviceWithPrefix;
        }
    }
}
