// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Newtonsoft.Json;

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

        private readonly IotHubServiceClient _hubClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly List<string> _deleteDevicesWithPrefix;

        public CleanupDevicesSample(IotHubServiceClient hubClient, BlobContainerClient sc, List<string> deleteDevicesWithPrefix)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
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
                    Uri storageAccountSasUri = GetStorageAccountSasUriForCleanupJob(_blobContainerClient);

                    // Step 3: Call import using the same blob to delete all devices.
                    var importDevicesToBeDeletedRequest = new JobProperties(storageAccountSasUri)
                    {
                        InputBlobName = ImportExportDevicesFileName,
                        StorageAuthenticationType = StorageAuthenticationType.KeyBased,
                    };

                    JobProperties importDevicesToBeDeletedJob = null;

                    Stopwatch jobTimer = Stopwatch.StartNew();
                    do
                    {
                        try
                        {
                            importDevicesToBeDeletedJob = await _hubClient.Devices.ImportAsync(importDevicesToBeDeletedRequest);
                            currentJobId = importDevicesToBeDeletedJob.JobId;
                            break;
                        }
                        // Wait for pending jobs to finish.
                        catch (IotHubServiceException ex)
                        {
                            Console.WriteLine($"Error submitting import job {ex.StatusCode}/{ex.ErrorCode}, {ex.Message}; waiting...");
                            await Task.Delay(s_waitDuration);
                        }
                    } while (jobTimer.Elapsed < s_maxJobDuration);

                    // Wait until job is finished.
                    jobTimer.Restart();
                    while (importDevicesToBeDeletedJob != null
                        && jobTimer.Elapsed < s_maxJobDuration)
                    {
                        importDevicesToBeDeletedJob = await _hubClient.Devices.GetJobAsync(importDevicesToBeDeletedJob.JobId);
                        if (importDevicesToBeDeletedJob.IsFinished)
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
                        await _hubClient.Devices.CancelJobAsync(currentJobId);
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

            int deviceCount = 0;

            try
            {
                string countSqlQuery = "select count() AS numberOfDevices from devices";
                QueryResponse<Dictionary<string, int>> countQuery = await _hubClient.Query.CreateAsync<Dictionary<string, int>>(countSqlQuery);
                if (!await countQuery.MoveNextAsync())
                {
                    Console.WriteLine($"Failed to run device count query.");
                    return 0;
                }

                if (!countQuery.Current.TryGetValue("numberOfDevices", out deviceCount))
                {
                    Console.WriteLine($"Failed to get device count from query result.");
                    return 0;
                }

                Console.WriteLine($"Total # of devices in the hub: \n{deviceCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process device count query due to {ex.Message}");
            }

            return deviceCount;
        }

        private async Task<IReadOnlyList<ExportImportDevice>> GetDeviceIdsToDeleteAsync(int maxCount)
        {
            var devicesToDelete = new List<ExportImportDevice>(maxCount);

            const string queryText = "select deviceId FROM devices";
            QueryResponse<string> devicesQuery = await _hubClient.Query.CreateAsync<string>(queryText);
            while (await devicesQuery.MoveNextAsync())
            {
                string deviceId = devicesQuery.Current;
                foreach (string prefix in _deleteDevicesWithPrefix)
                {
                    if (deviceId.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    {
                        devicesToDelete.Add(new ExportImportDevice(new Device(deviceId), ImportMode.Delete));
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

