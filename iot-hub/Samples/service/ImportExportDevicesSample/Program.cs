// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        // To populate these variables, either:
        // - pass these values as command-prompt arguments
        // - set the corresponding environment variables (you can do this at the command line
        //     and then run the app from the same command line.
        // - create a launchSettings.json (see launchSettings.json.template) containing the variables
        // If you do the last one, you have to rename launchSettings.json.template to launchSettings.json
        //   for it to work.
        // If you use the last method, be sure not to check the file into github with the 
        //   connection strings still in it.

        //IoT Hub connection string. You can get this from the portal.
        // Log into https://azure.portal.com, go to Resources, find your hub and select it.
        // Then look for Shared Access Policies and select it. 
        // Then select IoThubowner and copy one of the connection strings.
        private static string _IoTHubConnectionString =
            Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING");

        // When copying data from one hub to another, this is the connection string
        //   to the destination hub, i.e. the new one.
        private static string _DestIoTHubConnectionString =
            Environment.GetEnvironmentVariable("DEST_IOTHUB_CONN_STRING");

        // Connection string to the storage account used to hold the imported or exported data.
        // Log into https://azure.portal.com, go to Resources, find your storage account and select it.
        // Select Access Keys and copy one of the connection strings.
        private static string _storageAccountConnectionString =
            Environment.GetEnvironmentVariable("STORAGE_CONN_STRING");

        public static void Main(string[] args)
        {
            //To use this sample, uncomment the bits you want to run in ImportExportDevicesSample.RunSampleAsync

            //The size of the hub you are using should be able to manage the number of devices 
            //  you want to create and test with.

            // Check and see if the environment variables were read. If not, check
            //   for command-line arguments -- if found, load them into the connection strings.
            // This is just another way to run the sample that lets you run it w/o 
            //   putting the connection strings in the code.
            if (string.IsNullOrEmpty(_IoTHubConnectionString) && args.Length> 0)
            {
                _IoTHubConnectionString = args[0];
            }
            if (string.IsNullOrEmpty(_DestIoTHubConnectionString) && args.Length > 1)
            {
                _DestIoTHubConnectionString = args[1];
            }
            if (string.IsNullOrEmpty(_storageAccountConnectionString) && args.Length > 2)
            {
                _storageAccountConnectionString = args[2];
            }

            ImportExportDevicesSample importExportDevicesSample =
                new ImportExportDevicesSample(_IoTHubConnectionString, _DestIoTHubConnectionString,
                _storageAccountConnectionString);

            try
            {

                importExportDevicesSample.RunSampleAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.Print("Error. Description = {0}", ex.Message);
            }

            Console.WriteLine("Finished.");
            Console.WriteLine();
            Console.Write("Press any key to continue.");
            Console.ReadLine();

        }
    }
}
