// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This class leverages the import and export IoT hub device identities feature to export all registered device identities,
    /// filter out the devices that should not be deleted, and execute bulk deletion using <see cref="ImportMode.Delete"/>.
    /// For more details, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-bulk-identity-mgmt"/>.
    /// </summary>
    public class CleanupDevicesSample
    {
        private const string ImportExportDevicesFileName = "devices.txt";

        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);
        private static readonly IReadOnlyList<JobStatus> s_completedJobs = new[]
        {
            JobStatus.Completed,
            JobStatus.Failed,
            JobStatus.Cancelled,
        };

        private readonly RegistryManager _registryManager;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly List<string> _deleteDevicesWithPrefix;

        public CleanupDevicesSample(RegistryManager rm, BlobContainerClient sc, List<string> deleteDevicesWithPrefix)
        {
            _registryManager = rm ?? throw new ArgumentNullException(nameof(rm));
            _blobContainerClient = sc ?? throw new ArgumentNullException(nameof(sc));

            _deleteDevicesWithPrefix = deleteDevicesWithPrefix;
        }

        public async Task RunCleanUpAsync()
        {
            // Get the count of ALL devices registered to this hub instance.
            await PrintDeviceCountAsync();

            // Filter the devices that should be deleted (based on their prefix) and delete them.
            await CleanupDevices();
        }

        private async Task CleanupDevices()
        {
            Console.WriteLine($"Using storage container {_blobContainerClient.Name}" +
                $" for exporting devices identities to and importing device identities from.");

            // Retrieve the SAS Uri that will be used to grant access to the storage containers.
            string storageAccountSasUri = GetStorageAccountSasUriForCleanupJob(_blobContainerClient).ToString();

            // Step 1: Export all device identities.
            JobProperties exportAllDevicesProperties = JobProperties
                .CreateForExportJob(
                    outputBlobContainerUri: storageAccountSasUri,
                    excludeKeysInExport: true,
                    storageAuthenticationType: StorageAuthenticationType.KeyBased);

            JobProperties exportAllDevicesJob = await _registryManager.ExportDevicesAsync(exportAllDevicesProperties);

            // Wait until the export job is finished.
            while (true)
            {
                exportAllDevicesJob = await _registryManager.GetJobAsync(exportAllDevicesJob.JobId);
                if (s_completedJobs.Contains(exportAllDevicesJob.Status))
                {
                    // Job has finished executing.
                    break;
                }

                Console.WriteLine($"Job {exportAllDevicesJob.JobId} is {exportAllDevicesJob.Status} with progress {exportAllDevicesJob.Progress}%");
                await Task.Delay(s_waitDuration);
            }
            Console.WriteLine($"Job {exportAllDevicesJob.JobId} is {exportAllDevicesJob.Status}.");

            if (exportAllDevicesJob.Status != JobStatus.Completed)
            {
                throw new Exception("Exporting devices failed, exiting.");
            }

            // Step 2: Download the exported devices list from the blob create in Step 1.
            BlobClient blobClient = _blobContainerClient.GetBlobClient(ImportExportDevicesFileName);
            BlobDownloadInfo download = await blobClient.DownloadAsync();
            IEnumerable<ExportImportDevice> exportedDevices = ImportExportDevicesHelpers.BuildExportImportDeviceFromStream(download.Content);

            // Step 3: Collect the devices that need to be deleted and update their ImportMode to be Delete.
            // Thie step will create an ExportImportDevice identity for each device/ module identity registered on hub.
            // If you hub instance has IoT Hub module or Edge module instances registered, then they will be counted as separate entities
            // from the corresponding IoT Hub device/ Edge device that they are associated with.
            // As a result, the count of ExportImportDevice identities to be deleted might be greater than the
            // count of IoT hub devices retrieved in PrintDeviceCountAsync().
            var devicesToBeDeleted = new List<ExportImportDevice>();
            foreach (var device in exportedDevices)
            {
                string deviceId = device.Id;
                foreach (string prefix in _deleteDevicesWithPrefix)
                {
                    if (deviceId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        devicesToBeDeleted.Add(device);
                    }
                }
            }
            devicesToBeDeleted
                .ForEach(device => device.ImportMode = ImportMode.Delete);
            Console.WriteLine($"Retrieved {devicesToBeDeleted.Count} devices for deletion.");

            if (devicesToBeDeleted.Count > 0)
            {
                // Step 3a: Write the new import data back to the blob.
                using Stream devicesFile = ImportExportDevicesHelpers.BuildDevicesStream(devicesToBeDeleted);
                await blobClient.UploadAsync(devicesFile, overwrite: true);

                // Step 3b: Call import using the same blob to delete all devices.
                JobProperties importDevicesToBeDeletedProperties = JobProperties
                    .CreateForImportJob(
                        inputBlobContainerUri: storageAccountSasUri,
                        outputBlobContainerUri: storageAccountSasUri,
                        storageAuthenticationType: StorageAuthenticationType.KeyBased);
                JobProperties importDevicesToBeDeletedJob = await _registryManager.ImportDevicesAsync(importDevicesToBeDeletedProperties);

                // Wait until job is finished.
                while (true)
                {
                    importDevicesToBeDeletedJob = await _registryManager.GetJobAsync(importDevicesToBeDeletedJob.JobId);
                    if (s_completedJobs.Contains(importDevicesToBeDeletedJob.Status))
                    {
                        // Job has finished executing.
                        break;
                    }

                    Console.WriteLine($"Job {importDevicesToBeDeletedJob.JobId} is {importDevicesToBeDeletedJob.Status} with progress {importDevicesToBeDeletedJob.Progress}%");
                    await Task.Delay(s_waitDuration);
                }
                Console.WriteLine($"Job {importDevicesToBeDeletedJob.JobId} is {importDevicesToBeDeletedJob.Status}.");
            }

            // Step 4: Delete the storage container created.
            await _blobContainerClient.DeleteAsync();
            Console.WriteLine($"Storage container {_blobContainerClient.Name} deleted.");
        }

        private async Task PrintDeviceCountAsync()
        {
            string countSqlQuery = "SELECT COUNT() AS numberOfDevices FROM devices";
            IQuery countQuery = _registryManager.CreateQuery(countSqlQuery);
            while (countQuery.HasMoreResults)
            {
                IEnumerable<string> result = await countQuery.GetNextAsJsonAsync();
                Console.WriteLine($"Total # of devices in the hub: \n{result.First()}");
            }
        }

        private Uri GetStorageAccountSasUriForCleanupJob(BlobContainerClient blobContainerClient)
        {
            // We want to provide "Read", "Write" and "Delete" permissions to the storage container, so that it can
            // create a blob, read it and subsequently delete it.
            BlobContainerSasPermissions sasPermissions = BlobContainerSasPermissions.Write
                | BlobContainerSasPermissions.Read
                | BlobContainerSasPermissions.Delete;

            BlobSasBuilder sasBuilder = new BlobSasBuilder(sasPermissions, DateTimeOffset.UtcNow.AddHours(1))
            {
                BlobContainerName = blobContainerClient.Name,
            };

            return blobContainerClient.GenerateSasUri(sasBuilder);
        }
    }
}

