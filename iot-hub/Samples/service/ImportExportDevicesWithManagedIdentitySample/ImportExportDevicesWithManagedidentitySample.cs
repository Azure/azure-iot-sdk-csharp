// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
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
    public class ImportExportDevicesWithManagedidentitySample
    {
        public async Task RunSampleAsync(string sourceHubConnectionString,
            string destinationHubConnectionString,
            string blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            Console.WriteLine($"Exporting devices from source hub to {blobContainerUri}/devices.txt.");
            await ExportDevicesAsync(sourceHubConnectionString,
                blobContainerUri,
                userDefinedManagedIdentityResourceId);
            Console.WriteLine("Exporting devices completed.");

            Console.WriteLine($"Importing devices from {blobContainerUri}/devices.txt to destination hub.");
            await ImportDevicesAsync(destinationHubConnectionString,
                blobContainerUri,
                userDefinedManagedIdentityResourceId);
            Console.WriteLine("Importing devices completed.");
        }

        public async Task ExportDevicesAsync(string hubConnectionString,
            string blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            using RegistryManager srcRegistryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

            // If StorageAuthenticationType is set to IdentityBased and userAssignedIdentity property is
            // not null, the jobs will use user defined managed identity. If the IoT hub is not
            // configured with the user defined managed identity specified in userAssignedIdentity,
            // the job will fail.
            // If StorageAuthenticationType is set to IdentityBased and userAssignedIdentity property is
            // null, the jobs will use system defined identity by default. If the IoT hub is configured with the
            // system defined managed identity, the job will succeed but will not use the user defined managed identity.
            // If the IoT hub is not configured with system defined managed identity, the job will fail.
            // If StorageAuthenticationType is set to IdentityBased and neither user defined nor system defined
            // managed identities are configured on the hub, the job will fail.
            JobProperties jobProperties = JobProperties.CreateForExportJob(
                outputBlobContainerUri: blobContainerUri,
                excludeKeysInExport: false,
                storageAuthenticationType: StorageAuthenticationType.IdentityBased,
                identity: new ManagedIdentity
                {
                    userAssignedIdentity = userDefinedManagedIdentityResourceId
                });

            JobProperties jobResult = await srcRegistryManager
                .ExportDevicesAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                jobResult = await srcRegistryManager.GetJobAsync(jobResult.JobId);
                if (jobResult.Status == JobStatus.Completed)
                {
                    break;
                }
                else if (jobResult.Status == JobStatus.Failed)
                {
                    throw new Exception("Export job failed.");
                }
                else if (jobResult.Status == JobStatus.Cancelled)
                {
                    throw new Exception("Export job was canceled.");
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        public async Task ImportDevicesAsync(string hubConnectionString,
            string blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            using RegistryManager destRegistryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

            // If StorageAuthenticationType is set to IdentityBased and userAssignedIdentity property is
            // not null, the jobs will use user defined managed identity. If the IoT hub is not
            // configured with the user defined managed identity specified in userAssignedIdentity,
            // the job will fail.
            // If StorageAuthenticationType is set to IdentityBased and userAssignedIdentity property is
            // null, the jobs will use system defined identity by default. If the IoT hub is configured with the
            // system defined managed identity, the job will succeed but will not use the user defined managed identity.
            // If the IoT hub is not configured with system defined managed identity, the job will fail.
            // If StorageAuthenticationType is set to IdentityBased and neither user defined nor system defined
            // managed identities are configured on the hub, the job will fail.
            JobProperties jobProperties = JobProperties.CreateForImportJob(
                inputBlobContainerUri: blobContainerUri,
                outputBlobContainerUri: blobContainerUri,
                storageAuthenticationType: StorageAuthenticationType.IdentityBased,
                identity: new ManagedIdentity
                {
                    userAssignedIdentity = userDefinedManagedIdentityResourceId
                });

            JobProperties jobResult = await destRegistryManager
                .ImportDevicesAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                jobResult = await destRegistryManager.GetJobAsync(jobResult.JobId);
                if (jobResult.Status == JobStatus.Completed)
                {
                    break;
                }
                else if (jobResult.Status == JobStatus.Failed)
                {
                    throw new Exception("Import job failed.");
                }
                else if (jobResult.Status == JobStatus.Cancelled)
                {
                    throw new Exception("Import job was canceled.");
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
