// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = true,
            HelpText = "The connection string of the IoT Hub instance to connect to.")]
        public string HubConnectionString { get; set; }

        [Option(
            'a',
            "StorageAccountConnectionString",
            Required = true,
            HelpText = "The connection string for the Storage account where the device identities will be exported to and imported from.")]
        public string StorageAccountConnectionString { get; set; }

        [Option(
            "PathToDevicePrefixForDeletion",
            Required = true,
            HelpText = "The path to the file which contains the prefixes of the devices that are to be deleted.")]
        public string PathToDevicePrefixForDeletion { get; set; }
    }
}