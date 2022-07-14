// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly string ImportExportDevicesFileName = $"delete-devices-{Guid.NewGuid()}.txt";
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_maxJobDuration = TimeSpan.FromHours(4);

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
            Console.WriteLine($"Delete devices with prefixes: {JsonConvert.SerializeObject(deleteDevicesWithPrefix)}");
            _deleteDevicesWithPrefix = deleteDevicesWithPrefix;
        }

        public async Task RunCleanUpAsync()
        {
            // Get the count of ALL devices registered to this hub instance.
            int count = await PrintDeviceCountAsync();

            // Filter the devices that should be deleted (based on their prefix) and delete them.
            await CleanupDevices(count);
        }

        private async Task CleanupDevices(int deviceCount)
        {
            Console.WriteLine($"Using storage container {_blobContainerClient.Name} for importing device delete requests.");

            // Step 1: Collect the devices that need to be deleted.
            IReadOnlyList<ExportImportDevice> devicesToBeDeleted = await GetDeviceIdsToDeleteAsync(deviceCount);
            Console.WriteLine($"Discovered {devicesToBeDeleted.Count} devices for deletion.");

            string currentJobId = null;
            if (devicesToBeDeleted.Any())
            {
                try
                {
                    // Step 2: Write the new import data back to the blob.
                    using Stream devicesFile = ImportExportDevicesHelpers.BuildDevicesStream(devicesToBeDeleted);

                    // Retrieve the SAS Uri that will be used to grant access to the storage containers.
                    BlobClient blobClient = _blobContainerClient.GetBlobClient(ImportExportDevicesFileName);
                    var uploadResult = await blobClient.UploadAsync(devicesFile, overwrite: true);
                    string storageAccountSasUri = GetStorageAccountSasUriForCleanupJob(_blobContainerClient).ToString();

                    // Step 3: Call import using the same blob to delete all devices.
                    JobProperties importDevicesToBeDeletedProperties = JobProperties
                        .CreateForImportJob(
                            inputBlobContainerUri: storageAccountSasUri,
                            outputBlobContainerUri: storageAccountSasUri,
                            inputBlobName: ImportExportDevicesFileName,
                            storageAuthenticationType: StorageAuthenticationType.KeyBased);

                    JobProperties importDevicesToBeDeletedJob = null;

                    Stopwatch jobTimer = Stopwatch.StartNew();
                    do
                    {
                        try
                        {
                            importDevicesToBeDeletedJob = await _registryManager.ImportDevicesAsync(importDevicesToBeDeletedProperties);
                            currentJobId = importDevicesToBeDeletedJob.JobId;
                            break;
                        }
                        // Wait for pending jobs to finish.
                        catch (JobQuotaExceededException)
                        {
                            Console.WriteLine($"JobQuotaExceededException... waiting.");
                            await Task.Delay(s_waitDuration);
                        }
                    } while (jobTimer.Elapsed < s_maxJobDuration);

                    // Wait until job is finished.
                    jobTimer.Restart();
                    while (importDevicesToBeDeletedJob != null
                        && jobTimer.Elapsed < s_maxJobDuration)
                    {
                        importDevicesToBeDeletedJob = await _registryManager.GetJobAsync(importDevicesToBeDeletedJob.JobId);
                        if (s_completedJobs.Contains(importDevicesToBeDeletedJob.Status))
                        {
                            // Job has finished executing.
                            Console.WriteLine($"Job {importDevicesToBeDeletedJob.JobId} is {importDevicesToBeDeletedJob.Status}.");
                            currentJobId = null;
                            break;
                        }

                        Console.WriteLine($"Job {importDevicesToBeDeletedJob.JobId} is {importDevicesToBeDeletedJob.Status} after {jobTimer.Elapsed}.");
                        await Task.Delay(s_waitDuration);
                    }

                    if (importDevicesToBeDeletedJob?.Status != JobStatus.Completed)
                    {
                        throw new Exception("Importing devices job failed; exiting.");
                    }
                }
                finally
                {
                    if (!String.IsNullOrWhiteSpace(currentJobId))
                    {
                        Console.WriteLine($"Cancelling job {currentJobId}");
                        await _registryManager.CancelJobAsync(currentJobId);
                    }
                }
            }

            // Step 4: Delete the storage container created.
            await _blobContainerClient.DeleteAsync();
            Console.WriteLine($"Storage container {_blobContainerClient.Name} deleted.");
        }

        private async Task<int> PrintDeviceCountAsync()
        {
            //{
            //    "numberOfDevices": 10456
            //}

            string queryResultText = null;
            int deviceCount = 0;

            try
            {
                string countSqlQuery = "select count() AS numberOfDevices from devices";
                IQuery countQuery = _registryManager.CreateQuery(countSqlQuery);
                IEnumerable<string> queryResult = await countQuery.GetNextAsJsonAsync();
                queryResultText = queryResult.First();
                var resultObject = JObject.Parse(queryResultText);
                deviceCount = resultObject.Value<int>("numberOfDevices");
                Console.WriteLine($"Total # of devices in the hub: \n{deviceCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse device count of {queryResultText} due to {ex}");
            }

            return deviceCount;
        }

        private async Task<IReadOnlyList<ExportImportDevice>> GetDeviceIdsToDeleteAsync(int maxCount)
        {
            var devicesToDelete = new List<ExportImportDevice>(maxCount);

            const string queryText = "select deviceId FROM devices";
            IQuery devicesQuery = _registryManager.CreateQuery(queryText);
            while (devicesQuery.HasMoreResults)
            {
                IEnumerable<string> results = await devicesQuery.GetNextAsJsonAsync();
                foreach (string result in results)
                {
                    var resultObject = JObject.Parse(result);
                    string deviceId = resultObject.Value<string>("deviceId");
                    foreach (string prefix in _deleteDevicesWithPrefix)
                    {
                        if (deviceId.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            devicesToDelete.Add(new ExportImportDevice(new Device(deviceId), ImportMode.Delete));
                        }
                    }
                }
            }

            return devicesToDelete;
        }

        private static Uri GetStorageAccountSasUriForCleanupJob(BlobContainerClient blobContainerClient)
        {
            // We want to provide "Read", "Write" and "Delete" permissions to the storage container, so that it can
            // create a blob, read it and subsequently delete it.
            BlobContainerSasPermissions sasPermissions = BlobContainerSasPermissions.Write
                | BlobContainerSasPermissions.Read
                | BlobContainerSasPermissions.Delete;

            var sasBuilder = new BlobSasBuilder(sasPermissions, DateTimeOffset.UtcNow.AddHours(1))
            {
                BlobContainerName = blobContainerClient.Name,
            };

            return blobContainerClient.GenerateSasUri(sasBuilder);
        }
    }
}

