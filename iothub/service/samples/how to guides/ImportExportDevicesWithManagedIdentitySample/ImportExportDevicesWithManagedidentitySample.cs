﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

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
    internal class ImportExportDevicesWithManagedidentitySample
    {
        public async Task RunSampleAsync(string sourceHubConnectionString,
            string destinationHubConnectionString,
            Uri blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            Console.WriteLine($"Exporting devices from source hub to {blobContainerUri}/devices.txt.");
            await ExportDevicesAsync(sourceHubConnectionString,
                blobContainerUri,
                userDefinedManagedIdentityResourceId);
            Console.WriteLine("Exporting devices completed.");

            Console.WriteLine($"Importing devices from {blobContainerUri}/devices.txt to destination hub.");
            await ImportDevicesAsync(
                destinationHubConnectionString,
                blobContainerUri,
                userDefinedManagedIdentityResourceId);
            Console.WriteLine("Importing devices completed.");
        }

        public async Task ExportDevicesAsync(string hubConnectionString,
            Uri blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            using var client = new IotHubServiceClient(hubConnectionString);

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
            var jobProperties = new ExportJobProperties(blobContainerUri, false)
            {
                StorageAuthenticationType = StorageAuthenticationType.IdentityBased,
                Identity = new ManagedIdentity
                {
                    UserAssignedIdentity = userDefinedManagedIdentityResourceId
                },
            };

            IotHubJobResponse jobResult = await client.Devices.ExportAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                jobResult = await client.Devices.GetJobAsync(jobResult.JobId);
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
            Uri blobContainerUri,
            string userDefinedManagedIdentityResourceId = null)
        {
            using var client = new IotHubServiceClient(hubConnectionString);

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
            var jobProperties = new ImportJobProperties(blobContainerUri)
            {
                StorageAuthenticationType = StorageAuthenticationType.IdentityBased,
                Identity = new ManagedIdentity
                {
                    UserAssignedIdentity = userDefinedManagedIdentityResourceId
                },
            };

            ImportJobProperties importJobResult = await client.Devices.ImportAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                IotHubJobResponse jobResult = await client.Devices.GetJobAsync(importJobResult.JobId);
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
