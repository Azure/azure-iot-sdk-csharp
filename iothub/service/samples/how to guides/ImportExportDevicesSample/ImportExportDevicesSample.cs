// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    public class ImportExportDevicesSample
    {
        private const int BlobWriteBytes = 500;

        private readonly string _srcIotHubConnectionString;
        private readonly string _destIotHubConnectionString;
        private readonly string _storageAccountConnectionString;
        private readonly string _containerName;
        private readonly string _blobNamePrefix;

        private readonly string _generateDevicesBlobName;
        private readonly string _srcHubDevicesExportBlobName;
        private readonly string _srcHubConfigsExportBlobName;
        private readonly string _destHubDevicesImportBlobName;
        private readonly string _destHubConfigsImportBlobName;
        private readonly string _hubDevicesCleanupBlobName;

        private const string DeviceImportErrorsBlobName = "importErrors.log";
        private const string ConfigImportErrorsBlobName = "importConfigErrors.log";

        // The container used to hold the blob containing the list of import/export files.
        // This is a sample-wide variable. If this project doesn't find this container, it will create it.
        private BlobContainerClient _blobContainerClient;

        private Uri _containerUri;

        public ImportExportDevicesSample(
            string sourceIotHubConnectionString,
            string destinationHubConnectionString,
            string sourceStorageAccountConnectionString,
            string containerName,
            string blobNamePrefix)
        {
            _destIotHubConnectionString = destinationHubConnectionString;
            _srcIotHubConnectionString = sourceIotHubConnectionString;
            _storageAccountConnectionString = sourceStorageAccountConnectionString;
            _containerName = containerName;
            _blobNamePrefix = blobNamePrefix;

            _generateDevicesBlobName = _blobNamePrefix + "Generate.txt";
            _srcHubDevicesExportBlobName = _blobNamePrefix + "ExportDevices.txt";
            _srcHubConfigsExportBlobName = _blobNamePrefix + "ExportConfigs.txt";
            _destHubDevicesImportBlobName = _blobNamePrefix + "ImportDevices.txt";
            _destHubConfigsImportBlobName = _blobNamePrefix + "ImportConfigs.txt";
            _hubDevicesCleanupBlobName = _blobNamePrefix + "DeleteDevices.txt";
        }

        public async Task RunSampleAsync(
            int devicesToAdd,
            bool includeConfigurations,
            bool shouldCopyDevices,
            bool shouldDeleteSourceDevices,
            bool shouldDeleteDestDevices)
        {
            using var srcHubClient = new IotHubServiceClient(_srcIotHubConnectionString);
            using var destHubClient = new IotHubServiceClient(_destIotHubConnectionString);

            // This sets cloud blob container and returns container URI (w/shared access token).
            await PrepareStorageForImportExportAsync(_storageAccountConnectionString);

            if (devicesToAdd > 0)
            {
                // generate and add new devices
                await GenerateDevicesAsync(srcHubClient.Devices, devicesToAdd);

                if (includeConfigurations)
                {
                    await GenerateConfigurationAsync(srcHubClient.Configurations);
                }
            }

            if (shouldCopyDevices)
            {
                // Copy devices from the original hub to a new hub
                await CopyToDestHubAsync(srcHubClient.Devices, destHubClient.Devices, includeConfigurations);
            }

            if (shouldDeleteSourceDevices)
            {
                // delete devices from the source hub
                await DeleteFromHubAsync(srcHubClient, includeConfigurations);
            }

            if (shouldDeleteDestDevices)
            {
                // delete devices from the destination hub
                await DeleteFromHubAsync(destHubClient, includeConfigurations);
            }
        }

        /// <summary>
        /// Sets up references to the blob hierarchy objects, sets containerURI with an SAS for access.
        /// Create the container if it doesn't exist.
        /// </summary>
        /// <returns>URI to blob container, including SAS token</returns>
        private async Task PrepareStorageForImportExportAsync(string storageAccountConnectionString)
        {
            Console.WriteLine("Preparing storage.");

            try
            {
                // Get reference to storage account.
                // This is the storage account used to hold the import and export file lists.
                var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);

                _blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);

                // Get the URI to the container.
                _containerUri = _blobContainerClient
                    .GenerateSasUri(
                        BlobContainerSasPermissions.Write
                            | BlobContainerSasPermissions.Read
                            | BlobContainerSasPermissions.Delete,
                        DateTime.UtcNow.AddHours(24));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up storage account. Msg = {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Add devices to the hub; specify how many. This creates a number of devices
        ///   with partially random hub names.
        /// This is a good way to test the import -- create a bunch of devices on one hub,
        ///    then use this the Copy feature to copy the devices to another hub.
        /// Number of devices to create and add. Default is 10.
        /// </summary>
        private async Task GenerateDevicesAsync(DevicesClient devicesClient, int numToAdd)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Creating {numToAdd} devices for the source IoT hub.");
            int interimProgressCount = 0;
            int displayProgressCount = 1000;
            int totalProgressCount = 0;

            // generate reference for list of new devices we're going to add, will write list to this blob
            BlobClient generateDevicesBlob = _blobContainerClient.GetBlobClient(_generateDevicesBlobName);

            // define serializedDevices as a generic list<string>
            var serializedDevices = new List<string>(numToAdd);

            for (int i = 1; i <= numToAdd; i++)
            {
                // Create device name with this format: Hub_00000000 + a new guid.
                // This should be large enough to display the largest number (1 million).
                string deviceName = $"Hub_{i:D8}_{Guid.NewGuid()}";
                Debug.Print($"Adding device '{deviceName}'");

                // Create a new ExportImportDevice.
                var deviceToAdd = new ExportImportDevice
                {
                    Id = deviceName,
                    Status = DeviceStatus.Enabled,
                    Authentication = new AuthenticationMechanism
                    {
                        SymmetricKey = new SymmetricKey
                        {
                            PrimaryKey = GenerateKey(32),
                            SecondaryKey = GenerateKey(32),
                        }
                    },
                    // This indicates that the entry should be added as a new device.
                    ImportMode = ImportMode.Create,
                };

                // Add device to the list as a serialized object.
                serializedDevices.Add(JsonConvert.SerializeObject(deviceToAdd));

                // Not real progress as you write the new devices, but will at least show *some* progress.
                interimProgressCount++;
                totalProgressCount++;
                if (interimProgressCount >= displayProgressCount)
                {
                    Console.WriteLine($"Added {totalProgressCount}/{numToAdd} devices.");
                    interimProgressCount = 0;
                }
            }

            // Now have a list of devices to be added, each one has been serialized.
            // Write the list to the blob.
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice => sb.AppendLine(serializedDevice));

            // Write list of serialized objects to the blob.
            using Stream stream = await generateDevicesBlob.OpenWriteAsync(overwrite: true);
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            for (int i = 0; i < bytes.Length; i += BlobWriteBytes)
            {
                int length = Math.Min(bytes.Length - i, BlobWriteBytes);
                await stream.WriteAsync(bytes.AsMemory(i, length));
            }
            await stream.FlushAsync();

            Console.WriteLine("Running a registry manager job to add the devices.");

            // Should now have a file with all the new devices in it as serialized objects in blob storage.
            // generatedListBlob has the list of devices to be added as serialized objects.
            // Call import using the blob to add the new devices.
            // Log information related to the job is written to the same container.
            // This normally takes 1 minute per 100 devices (according to the docs).

            // First, initiate an import job.
            // This reads in the rows from the text file and writes them to IoT Devices.
            // If you want to add devices from a file, you can create a file and use this to import it.
            //   They have to be in the exact right format.
            try
            {
                // The first URI is the container to import from; the file defaults to devices.txt, but may be specified.
                // The second URI points to the container to write errors to as a blob.
                // This lets you import the devices from any file name. Since we wrote the new
                // devices to [devicesToAdd], need to read the list from there as well.
                var importGeneratedDevicesJob = new ImportJobProperties(_containerUri)
                {
                    InputBlobName = _generateDevicesBlobName,
                };
                ImportJobProperties jobResponse = await devicesClient.ImportAsync(importGeneratedDevicesJob);
                await WaitForJobAsync(devicesClient, jobResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Adding devices failed due to {ex.Message}");
            }

            stopwatch.Stop();
            Console.WriteLine($"GenerateDevices, time elapsed = {stopwatch.Elapsed}.");
        }

        private static async Task GenerateConfigurationAsync(ConfigurationsClient configClient)
        {
            Console.WriteLine($"Creating a configuration for the source IoT hub.");
            try
            {
                var config = new Configuration($"hub_config_{Guid.NewGuid().ToString().ToLower()}")
                {
                    Priority = 2,
                    Labels = { { "labelName", "labelValue" } },
                    TargetCondition = "*",
                    Content = new ConfigurationContent
                    {
                        DeviceContent = { { "properties.desired.x", 4L } },
                    },
                    Metrics = new ConfigurationMetrics
                    {
                        Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 4" } }
                    },
                };
                await configClient.CreateAsync(config);
                Debug.Print($"Added configuration '{config.Id}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding configuration. Exception message {ex.Message}");
            }
        }

        /// <summary>
        /// Copy the devices from one hub to another.
        /// </summary>
        /// <param name="sourceHubConnectionString">Connection string for source hub.</param>
        /// <param name="destHubConnectionString">Connection string for destination hub.</param>
        private async Task CopyToDestHubAsync(DevicesClient srcDevicesClient, DevicesClient destDevicesClient, bool includeConfigurations)
        {
            Console.WriteLine("Copying devices from the source to the destination IoT hub.");
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Exporting devices to destination IoT hub.");

            // Read the devices from the hub and write them to blob storage.
            await ExportDevicesAsync(srcDevicesClient, _srcHubDevicesExportBlobName, _srcHubConfigsExportBlobName, includeConfigurations);

            // Load the exported devices and edit the entries to be ready for import
            await LoadAndUpdateDevicesAsync(_srcHubDevicesExportBlobName, _destHubDevicesImportBlobName);
            await LoadAndUpdateConfigsAsync(_srcHubConfigsExportBlobName, _destHubConfigsImportBlobName);
            await ImportDevicesAsync(destDevicesClient, includeConfigurations);

            stopwatch.Stop();
            Console.WriteLine($"Copied devices: time elapsed = {stopwatch.Elapsed}");
        }

        /// Get the list of devices registered to the IoT hub
        ///   and export it to a blob as deserialized objects.
        private async Task ExportDevicesAsync(DevicesClient devicesClient, string devicesBlobName, string configsBlobName, bool includeConfigurations)
        {
            try
            {
                Console.WriteLine("Running a registry manager job to export devices from the hub.");

                // Call an export job on the IoT hub to retrieve all devices.
                // This writes them to the container.
                var exportJob = new ExportJobProperties(_containerUri, true)
                {
                    OutputBlobName = devicesBlobName,
                    IncludeConfigurations = includeConfigurations,
                    ConfigurationsBlobName = configsBlobName,
                };
                ExportJobProperties jobResponse = await devicesClient.ExportAsync(exportJob);
                await WaitForJobAsync(devicesClient, jobResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting devices to blob storage. Exception message = {ex.Message}");
            }
        }

        private async Task ImportDevicesAsync(DevicesClient devicesClient, bool includeConfigurations)
        {
            Console.WriteLine("Running a registry manager job to import the entries from the devices file to the destination IoT hub.");

            // Step 3: Call import using the same blob to create all devices.
            // Loads and adds the devices to the destination IoT hub.
            var importJob = new ImportJobProperties(_containerUri)
            {
                InputBlobName = _destHubDevicesImportBlobName,
                IncludeConfigurations = includeConfigurations,
                ConfigurationsBlobName = _destHubConfigsImportBlobName
            };
            var jobResponse = await devicesClient.ImportAsync(importJob);
            await WaitForJobAsync(devicesClient, jobResponse);

            // Read from error logs to see if there were any failures
            BlobClient devicesErrorsBlob = _blobContainerClient.GetBlobClient(DeviceImportErrorsBlobName);
            await PrintErrorsAsync(devicesErrorsBlob);
            BlobClient configErrorsBlob = _blobContainerClient.GetBlobClient(ConfigImportErrorsBlobName);
            await PrintErrorsAsync(configErrorsBlob);
        }

        private static async Task PrintErrorsAsync(BlobClient errorsBlob)
        {
            List<string> errors = await ReadFromBlobAsync(errorsBlob);
            if (errors.Any())
            {
                Console.WriteLine($"Found reported errors importing:");
                foreach (string error in errors)
                {
                    Console.WriteLine($"\t{error}");
                }
            }
        }

        /// <summary>
        /// Delete all of the devices from the hub with the given connection string.
        /// </summary>
        /// <remarks>
        /// This shows how to delete all of the devices for the IoT hub.
        /// First, export the list storage. (ExportDevices).
        /// Next, read in that file. Each row is a serialized object;
        /// read them into the generic list serializedDevices.
        /// For each serializedDevice, deserialize it, set ImportMode to Delete,
        /// reserialize it, and write it to a StringBuilder. The ImportMode field is what
        /// tells the job framework to delete each one.
        /// Write the new StringBuilder to the block blob.
        /// This essentially replaces the list with a list of devices that have ImportJob = Delete.
        /// Call ImportDevicesAsync, which will read in the list from storage, then delete each one.
        /// </remarks>
        /// <param name="hubConnectionString">Connection to the hub from which you want to delete the devices.</param>
        private async Task DeleteFromHubAsync(IotHubServiceClient hubClient, bool includeConfigurations)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Deleting all devices from an IoT hub.");

            Console.WriteLine("Exporting a list of devices from IoT hub to blob storage.");

            // Read from storage, which contains serialized objects.
            // Write each line to the serializedDevices list.
            BlobClient devicesBlobClient = _blobContainerClient.GetBlobClient(_destHubDevicesImportBlobName);

            Console.WriteLine("Reading the list of devices in from blob storage.");
            List<string> serializedDevices = await ReadFromBlobAsync(devicesBlobClient);

            // Step 1: Update each device's ImportMode to be Delete
            Console.WriteLine("Updating ImportMode to be 'Delete' for each device and writing back to the blob.");
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedEntity =>
            {
                // Deserialize back to an ExportImportDevice and change import mode.
                ExportImportDevice device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedEntity);
                device.ImportMode = ImportMode.Delete;

                // Reserialize the object now that we've updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Write the list in memory to the blob.
            BlobClient deleteDevicesBlobClient = _blobContainerClient.GetBlobClient(_hubDevicesCleanupBlobName);
            await WriteToBlobAsync(deleteDevicesBlobClient, sb.ToString());

            // Step 3: Call import using the same blob to delete all devices.
            Console.WriteLine("Running a registry manager job to delete the devices from the IoT hub.");
            var importJob = new ImportJobProperties(_containerUri)
            {
                InputBlobName = _hubDevicesCleanupBlobName,
            };
            ImportJobProperties ImportJobResponse = await hubClient.Devices.ImportAsync(importJob);
            await WaitForJobAsync(hubClient.Devices, ImportJobResponse);

            // Step 4: delete configurations
            if (includeConfigurations)
            {
                BlobClient configsBlobClient = _blobContainerClient.GetBlobClient(_srcHubConfigsExportBlobName);
                List<string> serializedConfigs = await ReadFromBlobAsync(configsBlobClient);
                foreach (string serializedConfig in serializedConfigs)
                {
                    try
                    {
                        Configuration config = JsonConvert.DeserializeObject<Configuration>(serializedConfig);
                        await hubClient.Configurations.DeleteAsync(config.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to deserialize or remove a config.\n\t{serializedConfig}\n\n{ex.Message}");
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Deleted IoT hub devices and configs: time elapsed = {stopwatch.Elapsed}");
        }

        private async Task LoadAndUpdateDevicesAsync(string readFromBlobName, string writeToBlobName)
        {
            // Write each line to the serializedDevices list.
            BlobClient readFromBlob = _blobContainerClient.GetBlobClient(readFromBlobName);

            Console.WriteLine("Reading in the list of devices from blob storage.");
            List<string> serializedDevices = await ReadFromBlobAsync(readFromBlob);

            Console.WriteLine("Updating ImportMode to be Create.");
            // Step 1: Update each device's ImportMode to Create
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice and update the import mode property.
                try
                {
                    ExportImportDevice device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                    device.ImportMode = ImportMode.Create;

                    // Reserialize the object now that we've updated the property.
                    sb.AppendLine(JsonConvert.SerializeObject(device));
                }
                catch
                {
                    // Exports can have warnings and other messages from the hub, so we have to handle lines that aren't actually
                    // serialized devices. This one is probably the warning that since we didn't include the credentials,
                    // if they are imported back into the same IoT hub, the credentials will be newly generated.
                    Debug.Print($"Export file contained a line that is not a device:\t`{serializedDevice}`");
                }
            });

            // Step 2: Write the in-memory list to the blob.
            BlobClient writeToBlob = _blobContainerClient.GetBlobClient(writeToBlobName);
            await WriteToBlobAsync(writeToBlob, sb.ToString());
        }

        private async Task LoadAndUpdateConfigsAsync(string readFromBlobName, string writeToBlobName)
        {
            // Write each line to the list.
            BlobClient readFromBlob = _blobContainerClient.GetBlobClient(readFromBlobName);

            Console.WriteLine("Reading in the list of configs from blob storage.");
            List<string> serializedDevices = await ReadFromBlobAsync(readFromBlob);

            Console.WriteLine("Updating config's ImportMode.");
            // Step 1: Update each config's ImportMode
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedConfig =>
            {
                // Deserialize back to an ExportImportDevice and update the import mode property.
                try
                {
                    ImportConfiguration config = JsonConvert.DeserializeObject<ImportConfiguration>(serializedConfig);
                    config.ImportMode = ConfigurationImportMode.CreateOrUpdateIfMatchETag;

                    // Reserialize the object now that we've updated the property.
                    sb.AppendLine(JsonConvert.SerializeObject(config));
                }
                catch
                {
                    // Exports can have warnings and other messages from the hub, so we have to handle lines that aren't actually
                    // serialized devices. This one is probably the warning that since we didn't include the credentials,
                    // if they are imported back into the same IoT hub, the credentials will be newly generated.
                    Debug.Print($"Export file contained a line that is not a config:\t`{serializedConfig}`");
                }
            });

            // Step 2: Write the in-memory list to the blob.
            BlobClient writeToBlob = _blobContainerClient.GetBlobClient(writeToBlobName);
            await WriteToBlobAsync(writeToBlob, sb.ToString());
        }

        private static async Task<List<string>> ReadFromBlobAsync(BlobClient blobClient)
        {
            // Read the blob file of devices, import each row into a list.
            var contents = new List<string>();

            using Stream blobStream = await blobClient.OpenReadAsync();
            using var streamReader = new StreamReader(blobStream, Encoding.UTF8);
            while (streamReader.Peek() != -1)
            {
                string line = await streamReader.ReadLineAsync();
                contents.Add(line);
            }

            return contents;
        }

        private static async Task WriteToBlobAsync(BlobClient blobClient, string contents)
        {
            using Stream stream = await blobClient.OpenWriteAsync(overwrite: true);
            byte[] bytes = Encoding.UTF8.GetBytes(contents);
            for (int i = 0; i < bytes.Length; i += BlobWriteBytes)
            {
                int length = Math.Min(bytes.Length - i, BlobWriteBytes);
                await stream.WriteAsync(bytes.AsMemory(i, length));
            }
            await stream.FlushAsync();
        }

        private static string GenerateKey(int keySize)
        {
            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = RandomNumberGenerator.Create();
            while (keyBytes.Contains(byte.MinValue))
            {
                cyptoProvider.GetBytes(keyBytes);
            }

            return Convert.ToBase64String(keyBytes);
        }

        private static async Task WaitForJobAsync(DevicesClient devicesClient, JobProperties job)
        {
            // Wait until job is finished
            while (true)
            {
                IotHubJobResponse jobResponse = await devicesClient.GetJobAsync(job.JobId);
                if (job.IsFinished)
                {
                    // Job has finished executing
                    Console.WriteLine($"Job finished with status of {jobResponse.Status}.");
                    break;
                }
                Console.WriteLine($"\tJob status is {jobResponse.Status}...");

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
