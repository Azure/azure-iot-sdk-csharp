// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace ImportExportDevicesWithManagedIdentitySample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            "sourceHubConnectionString",
            Required = true,
            HelpText = "The connection string of the source IoT Hub.")]
        public string SourceHubConnectionString { get; set; }

        [Option(
           "destinationHubConnectionString",
           Required = true,
           HelpText = "The connection string of the destination IoT Hub.")]
        public string DestinationHubConnectionString { get; set; }

        [Option(
            "blobContainerUri",
            Required = true,
            HelpText = "The Uri of storage container for import and export jobs.")]
        public string BlobContainerUri { get; set; }

        [Option(
            "userDefinedManagedIdentityResourceId",
            Required = false,
            HelpText = "The resource Id of the user defined managed identity. This is not required if you want to use system defined managed identity.")]
        public string UserDefinedManagedIdentityResourceId { get; set; }
    }
}
