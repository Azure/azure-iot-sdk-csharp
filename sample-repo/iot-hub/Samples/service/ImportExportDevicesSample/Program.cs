// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        // This application will do the following:
        //   * create new devices and add them to an IoT hub (for testing) -- you specify how many you want to add
        //      --> This has been tested up to 500,000 devices, 
        //          but should work all the way up to the million devices allowed on a hub.
        //   * copy the devices from one hub to another
        //   * delete the devices from any hub -- referred to as source or destination in case 
        //       you're cloning a hub and want to test adding or copying devices more than once
        //
        // To specify which of the options you want to run,
        // set these booleans to true or false, depending on which bits you want to run. 
        // If you don't want them to run, set them to false.
        //
        // You can set environment variables for these, or pass them in as command-line arguments. 
        // They are the first five command-line arguments, and all are required 
        //   because args 6 through 8 are required connection strings. 
        // These are passed in as strings and converted to numeric or boolean, whichever the case may be.

        // Add randomly created devices to the source hub.
        private static bool _addDevices = false;
        //If you ask to add devices, this will be the number added.
        private static int _numToAdd = 0;
        // Copy the devices from the source hub to the destination hub.
        private static bool _copyDevices = false;
        // Delete all of the devices from the source hub. (It uses the IoTHubConnectionString).
        private static bool _deleteSourceDevices = false;
        // Delete all of the devices from the destination hub. (Uses the DestIotHubConnectionString).
        private static bool _deleteDestDevices = false;
        // You can also add these to the launchSettings.json file (see launchSettings.json.template),
        //  but you must be careful not to check that file in to source control and expose your secrets. 
        //
        // These are the connection strings to the hubs and the storage account. 
        // If you are cloning a hub, the source is the original hub and the destination is the clone.

        // These retrieve the environment variables. If they are null or empty, 
        //    it will try to get these from the command line arguments. 
        // You can set these on the command line with the SET command, like this:
        //    SET ADD_DEVICES=TRUE
        // Then you run the application from that command window and the environment variables
        //    will be available in the application.
        // For more information about this application's use, see the Clone-a-hub article here: 
        //   https://docs.microsoft.com/azure/iot-hub/iot-hub-how-to-clone
        private static string _envAddDevices = Environment.GetEnvironmentVariable("ADD_DEVICES");
        private static string _envNumToAdd = Environment.GetEnvironmentVariable("NUM_TO_ADD");
        private static string _envCopyDevices = Environment.GetEnvironmentVariable("COPY_DEVICES");
        private static string _envDeleteSourceDevices = Environment.GetEnvironmentVariable("DELETE_SOURCE_DEVICES");
        private static string _envDeleteDestDevices = Environment.GetEnvironmentVariable("DELETE_DEST_DEVICES");

        // IoT Hub connection string. You can get this from the portal.
        //   Log into https://azure.portal.com, go to Resources, find your hub and select it.
        //   Then look for Shared Access Policies and select it. 
        //   Then select IoThubowner and copy one of the connection strings.
        private static string _envIotHubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING");

        // When copying data from one hub to another, this is the connection string
        //   to the destination hub, i.e. the new one.
        private static string _envDestIotHubConnectionString = Environment.GetEnvironmentVariable("DEST_IOTHUB_CONN_STRING");

        // Connection string to the storage account used to hold the imported or exported data.
        //   Log into https://azure.portal.com, go to Resources, find your storage account and select it.
        //   Select Access Keys and copy one of the connection strings.
        private static string _envStorageAccountConnectionString = Environment.GetEnvironmentVariable("STORAGE_ACCT_CONN_STRING");

        public static async Task Main(string[] args)
        {
            //The size of the hub you are using should be able to manage the number of devices 
            //  you want to create and test with.

            // When retrieving the variables, it works like this:
            // For the 5 options, try to read the environment variable. These will be put in the 
            //   private static strings above, like _copy_Devices.
            // If the environment variable is blank and there is a command-line argument available, 
            //   read that argument into a string (the private static strings above, like _copy_Devices).
            // Convert the private static string to boolean or integer (depending on how it's defined)
            //   and store the result in the class-level camelcase variables (like copyDevices).

            // For the connection strings, try to read the environment variable. These will be put in the
            //   private static strings above, like _IoTHubConnectionString. 
            // If the connection string environment variable is blank, try to get it from the 
            //   command line argument. These don't require any conversions because they are all text.

            // Note that because of the order of the arguments, you can set the connection strings 
            //   using environment variables and then pass in the five options using command line args.
            // The only reason you might want to do this is because the connection strings are so long,
            //   it makes it hard to distinguish them at the command line.

            // If you want to look at the raw arg values, uncomment this and they will print in the console window.
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine($"args({i}) = {args[i]}");
            }
            // **addDevices**
            if (string.IsNullOrEmpty(_envAddDevices) && args.Length > 0)
            {
                _envAddDevices = args[0];
            }
            if (!string.IsNullOrEmpty(_envAddDevices))
            {
                _addDevices = _envAddDevices.Trim().ToUpper() == "TRUE" ? true : false;
            }

            // **numToAdd**
            if (_addDevices)
            {
                if (string.IsNullOrEmpty(_envNumToAdd) && args.Length > 1)
                {
                    _envNumToAdd = args[1];
                }
                if (!string.IsNullOrEmpty(_envNumToAdd))
                {
                    _ = int.TryParse(_envNumToAdd, out _numToAdd);
                }
            }

            // **copyDevices**
            if (string.IsNullOrEmpty(_envCopyDevices) && args.Length > 2)
            {
                _envCopyDevices = args[2];
            }
            if (!string.IsNullOrEmpty(_envCopyDevices))
            {
                _copyDevices = _envCopyDevices.Trim().ToUpper() == "TRUE" ? true : false;
            }
            // **deleteSourceDevices**
            if (string.IsNullOrEmpty(_envDeleteSourceDevices) && args.Length > 3)
            {
                _envDeleteSourceDevices = args[3].Trim().ToUpper();
            }
            if (!string.IsNullOrEmpty(_envDeleteSourceDevices))
            {
                _deleteSourceDevices = _envDeleteSourceDevices.Trim().ToUpper() == "TRUE" ? true : false;
            }

            // **deleteDestDevices**
            if (string.IsNullOrEmpty(_envDeleteDestDevices) && args.Length > 4)
            {
                _envDeleteDestDevices = args[4];
            }
            if (!string.IsNullOrEmpty(_envDeleteDestDevices))
            {
                _deleteDestDevices = _envDeleteDestDevices.Trim().ToUpper() == "TRUE" ? true : false;
            }

            // ** IoTHubConnectionString **
            if (string.IsNullOrEmpty(_envIotHubConnectionString) && args.Length > 5)
            {
                _envIotHubConnectionString = args[5];
            }

            // ** DestIoTHubConnectionString **
            if (string.IsNullOrEmpty(_envIotHubConnectionString) && args.Length > 5)
            {
                _envDestIotHubConnectionString = args[5];
            }

            // ** storageAccountConnectionString **
            if (string.IsNullOrEmpty(_envStorageAccountConnectionString) && args.Length > 5)
            {
                _envStorageAccountConnectionString = args[5];
            }

            // Show the data passed in and what it thinks it was.
            Console.WriteLine("Input:");
            Console.WriteLine($"  add devices = {_addDevices}.");
            Console.WriteLine($"  num to add = {_numToAdd}.");
            Console.WriteLine($"  copy devices = {_copyDevices}.");
            Console.WriteLine($"  delete source devices = {_deleteSourceDevices}.");
            Console.WriteLine($"  delete dest devices = {_deleteDestDevices}.");
            Console.WriteLine($"  IoTHubConnString = '{_envIotHubConnectionString}'.");
            Console.WriteLine($"  IoTHubDestString = '{_envDestIotHubConnectionString}'.");
            Console.WriteLine($"  storage connection string  = '{_envStorageAccountConnectionString}'.");

            try
            {
                // Instantiate the class and run the sample.
                var importExportDevicesSample = new ImportExportDevicesSample(
                    _envIotHubConnectionString,
                    _envDestIotHubConnectionString,
                    _envStorageAccountConnectionString);

                await importExportDevicesSample
                    .RunSampleAsync(_addDevices, _numToAdd, _copyDevices, _deleteSourceDevices, _deleteDestDevices)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error. Description = {ex.Message}");
                Console.WriteLine($"Error. Description = {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine("Finished. Press any key to continue.");
            Console.ReadKey(true);
        }
    }
}
