// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This class leverages the import and export IoT hub device identities feature to export all registered device identities,
    /// filter out the devices that should not be deleted, and execute bulk deletion using <see cref="ImportMode.Delete"/>.
    /// For more details, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-bulk-identity-mgmt"/>.
    /// </summary>
    public class CleanupDevicesSample
    {
        private const string ImportErrorsLog = "importErrors.log";

        private static readonly string s_importExportDevicesFileName = $"delete-devices-{Guid.NewGuid()}.txt";
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_maxJobDuration = TimeSpan.FromHours(4);

        private readonly IotHubServiceClient _hubClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly List<string> _saveDevicesWithPrefix;

        public CleanupDevicesSample(IotHubServiceClient hubClient, BlobContainerClient sc, List<string> saveDevicesWithPrefix)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
            _blobContainerClient = sc ?? throw new ArgumentNullException(nameof(sc));
            Console.WriteLine($"Delete devices without prefixes: {JsonSerializer.Serialize(saveDevicesWithPrefix)}");
            _saveDevicesWithPrefix = saveDevicesWithPrefix;
        }

        public async Task RunCleanUpAsync()
        {
            // Get the count of ALL devices registered to this hub instance.
            int count = await PrintDeviceCountAsync();

            // Filter the devices that should be deleted (based on their prefix) and delete them.
            await CleanupDevicesAsync(count);
        }

        private async Task CleanupDevicesAsync(int deviceCount)
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
                    BlobClient importDevicesBlobClient = _blobContainerClient.GetBlobClient(s_importExportDevicesFileName);
                    var uploadResult = await importDevicesBlobClient.UploadAsync(devicesFile, overwrite: true);
                    Uri storageAccountSasUri = GetStorageAccountSasUriForCleanupJob(_blobContainerClient);

                    // Step 3: Call import using the same blob to delete all devices.
                    var importDevicesToBeDeletedRequest = new ImportJobProperties(storageAccountSasUri)
                    {
                        InputBlobName = s_importExportDevicesFileName,
                        StorageAuthenticationType = StorageAuthenticationType.KeyBased,
                    };

                    var jobTimer = Stopwatch.StartNew();
                    do
                    {
                        try
                        {
                            ImportJobProperties importDevicesToBeDeletedJob = await _hubClient.Devices.ImportAsync(importDevicesToBeDeletedRequest);
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
                    IotHubJobResponse deleteDevicesJob = null;
                    while (jobTimer.Elapsed < s_maxJobDuration)
                    {
                        deleteDevicesJob = await _hubClient.Devices.GetJobAsync(currentJobId);
                        if (deleteDevicesJob.IsFinished)
                        {
                            // Job has finished executing.
                            Console.WriteLine($"\tJob {deleteDevicesJob.JobId} is {deleteDevicesJob.Status}.");
                            currentJobId = null;
                            break;
                        }

                        Console.WriteLine($"\tJob {deleteDevicesJob.JobId} is {deleteDevicesJob.Status} after {jobTimer.Elapsed}.");
                        await Task.Delay(s_waitDuration);
                    }

                    await DiscoverAndReportErrorsAsync();

                    if (deleteDevicesJob?.Status != JobStatus.Completed)
                    {
                        throw new Exception("Importing devices job failed; exiting.");
                    }
                }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(currentJobId))
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

        private async Task DiscoverAndReportErrorsAsync()
        {
            Console.WriteLine("Looking for any errors reported from import...");
            try
            {
                BlobClient importErrorsBlobClient = _blobContainerClient.GetBlobClient(ImportErrorsLog);

                var content = await importErrorsBlobClient.DownloadContentAsync();
                string errorContent = content.Value.Content.ToString();
                string[] errors = errorContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine($"Found {errors.Length} errors reported:");
                foreach (string error in errors)
                {
                    try
                    {
                        ImportError importError = JsonSerializer.Deserialize<ImportError>(error);
                        Console.WriteLine($"\tImport error for {importError.DeviceId} of code {importError.ErrorCode} with status: '{importError.ErrorStatus}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to deserialize an import error due to [{ex.Message}].");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to review errors due to [{ex.Message}].");
            }
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

                Console.WriteLine($"Total # of devices in the hub: {deviceCount:N0}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process device count query due to {ex.Message}");
            }

            return deviceCount;
        }

        private class DeviceQueryResult
        {
            public string DeviceId { get; set; }
        }

        private async Task<IReadOnlyList<ExportImportDevice>> GetDeviceIdsToDeleteAsync(int maxCount)
        {
            var devicesToDelete = new List<ExportImportDevice>(maxCount);

            Console.WriteLine($"Querying devices to find devices to delete based on their name and the provided allow list.");

            var queryTextSb = new StringBuilder("select deviceId from devices");
            if (_saveDevicesWithPrefix.Any())
            {
                queryTextSb.Append(" where");
                for (int i = 0; i < _saveDevicesWithPrefix.Count; i++)
                {
                    // only prepend an "and" after the first where clause
                    if (i != 0)
                    {
                        queryTextSb.Append(" and");
                    }

                    queryTextSb.Append($" not startswith(deviceId, '{_saveDevicesWithPrefix[i]}')");
                }
            }
            string queryText = queryTextSb.ToString();
            Console.WriteLine($"Using query: {queryText}");
            var options = new QueryOptions { PageSize = 1000 };
            QueryResponse<DeviceQueryResult> devicesQuery = await _hubClient.Query.CreateAsync<DeviceQueryResult>(queryText, options);
            while (await devicesQuery.MoveNextAsync())
            {
                devicesToDelete.Add(new ExportImportDevice(new Device(devicesQuery.Current.DeviceId), ImportMode.Delete));
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

            var sasBuilder = new BlobSasBuilder(sasPermissions, DateTimeOffset.UtcNow.AddHours(12))
            {
                BlobContainerName = blobContainerClient.Name,
            };

            return blobContainerClient.GenerateSasUri(sasBuilder);
        }
    }
}
