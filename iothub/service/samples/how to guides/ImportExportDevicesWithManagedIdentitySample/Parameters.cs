// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace ImportExportDevicesWithManagedIdentitySample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    /// <remarks>
    /// To get these connection strings, log into https://portal.azure.com, go to Resources, open the IoT hub, open Shared Access Policies, open iothubowner, and copy a connection string.
    /// </remarks>
    internal class Parameters
    {
        [Option(
            "sourceHubConnectionString",
            Required = true,
            HelpText = "The connection string of the source IoT hub.")]
        public string SourceHubConnectionString { get; set; }

        [Option(
           "destinationHubConnectionString",
           Required = true,
           HelpText = "The connection string of the destination IoT hub.")]
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
