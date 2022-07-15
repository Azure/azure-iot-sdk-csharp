// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace ImportExportDevicesWithManagedIdentitySample
{
    /// <summary>
    /// A sample to illustrate how to perform import and export jobs using managed identity 
    /// to access the storage account. This sample will copy all the devices in the source hub
    /// to the destination hub.
    /// For this sample to succeed, the managed identity should be configured to access the 
    /// storage account used for import and export.
    /// For more information on configuration, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-managed-identity"/>.
    /// For more information on managed identities, see <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview"/>
    /// </summary>
    public class Program
    {        
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            var sample = new ImportExportDevicesWithManagedidentitySample();

            await sample.RunSampleAsync(parameters.SourceHubConnectionString,
                parameters.DestinationHubConnectionString,
                parameters.BlobContainerUri,
                parameters.UserDefinedManagedIdentityResourceId);
        }

    }
}
