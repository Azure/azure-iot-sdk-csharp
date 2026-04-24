// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application supplied via command line arguments.
    /// If the parameter is not supplied via command line args, it will look for it in environment variables.
    /// </summary>
    /// <remarks>
    /// To get the connection string, log into https://azure.portal.com, go to Resources, open the IoT hub, open Shared Access Policies, open iothubowner, and copy a connection string.
    /// </remarks>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            HelpText = "The IoT hub connection string. "
                + "\nDefaults to environment variable 'IOTHUB_CONNECTION_STRING'.")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        [Option(
            'd',
            "DeviceId",
            HelpText = "The Id of the plug and play compatible device to be used in the sample." 
                + "\nDefaults to environment variable 'IOTHUB_DEVICE_ID'")]
        public string DeviceId { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_ID");

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(HubConnectionString)
                && !string.IsNullOrWhiteSpace(DeviceId);
        }
    }
}
