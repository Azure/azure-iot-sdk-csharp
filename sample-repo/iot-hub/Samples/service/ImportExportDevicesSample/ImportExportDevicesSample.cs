// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ImportExportDevicesSample
    {
        // Container used to hold the blob containing the list of import/export files.
        // This is a module-wide variable. If this project doesn't find this container, it will create it.
        private CloudBlobContainer _cloudBlobContainer;

        // Name of blob container holding the work data.
        private const string _containerName = "devicefiles";

        // Name of the file used for exports and imports. 
        // This is set by the IoT SDK, and can't be changed.
        private const string _deviceListFile = "devices.txt";

        private string _containerUriWithSas = string.Empty;

        private readonly string _iotHubConnectionString;
        private readonly string _destIotHubConnectionString;
        private readonly string _storageAccountConnectionString;

        public ImportExportDevicesSample(
            string sourceIotHubConnectionString,
            string destinationHubConnectionString,
            string sourceStorageAccountConnectionString)
        {
            _destIotHubConnectionString = destinationHubConnectionString;
            _iotHubConnectionString = sourceIotHubConnectionString;
            _storageAccountConnectionString = sourceStorageAccountConnectionString;
        }

        public async Task RunSampleAsync(
            bool addDevices,
            int numToAdd,
            bool copyDevices,
            bool deleteSourceDevices,
            bool deleteDestDevices)
        {
            // This sets cloud blob container and returns container uri (w/shared access token).
            _containerUriWithSas = await PrepareStorageForImportExportAsync(_storageAccountConnectionString).ConfigureAwait(false);

            if (addDevices)
            {
                // generate and add new devices 
                await AddNewDevices(_iotHubConnectionString, numToAdd).ConfigureAwait(false);
            }

            if (copyDevices)
            {
                // Copy devices from the original hub to a new hub
                await CopyToNewHub(_iotHubConnectionString, _destIotHubConnectionString).ConfigureAwait(false);
            }

            if (deleteSourceDevices)
            {
                // delete devices from the source hub
                await DeleteFromHub(_iotHubConnectionString).ConfigureAwait(false);
            }

            if (deleteDestDevices)
            {
                // delete devices from the destination hub
                await DeleteFromHub(_destIotHubConnectionString).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets up references to the blob hierarchy objects, sets containerURI with an SAS for access.
        /// Create the container if it doesn't exist.
        /// </summary>
        /// <returns>URI to blob container, including SAS token</returns>
        private async Task<string> PrepareStorageForImportExportAsync(string storageAccountConnectionString)
        {
            Console.WriteLine("Preparing storage.");

            string containerUri;
            try
            {
                // Get reference to storage account.
                // This is the storage account used to hold the import and export file lists.
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

                // Get reference to the blob client.
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                // Get reference to the container to be used.
                _cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);

                // Get the URI to the container. This doesn't have an SAS token (yet).
                containerUri = _cloudBlobContainer.Uri.ToString();
            }
            catch (Exception ex)
            {
                Debug.Print("Error setting up storage account. Msg = {0}", ex.Message);
                throw ex;
            }

            // How to get reference to a blob. 
            //   Just leaving this in here in case you want to copy this block of code and use it for this.
            // CloudBlockBlob cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(deviceListFile);
            // This is how you get the URI to the blob. The import and export both use a container
            //   with container-level access, so this isn't used for importing or exporting the devices.
            // Just leaving it in here for when it's needed.
            // string blobURI = blockBlob.Uri.ToString();

            try
            {
                // The call below will fail if the sample is configured to use the storage emulator 
                //   in the connection string but the emulator is not running.
                // Change the retry policy for this call so that if it fails, it fails quickly.
                BlobRequestOptions requestOptions = new BlobRequestOptions { RetryPolicy = new NoRetry() };
                await _cloudBlobContainer.CreateIfNotExistsAsync(requestOptions, null).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is StorageException))
            {
            }

            // Call to get the SAS token for container-level access.
            string containerSASToken = GetContainerSasToken(_cloudBlobContainer);

            // Append the SAS token to the URI to the container. This is returned.
            _containerUriWithSas = containerUri + containerSASToken;
            return _containerUriWithSas;
        }

        /// <summary>
        /// Create the SAS token for the container.
        /// </summary>
        private string GetContainerSasToken(CloudBlobContainer container)
        {
            // Set the expiry time and permissions for the container.
            // In this case no start time is specified, so the
            // shared access signature becomes valid immediately.
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write
                    | SharedAccessBlobPermissions.Read
                    | SharedAccessBlobPermissions.Delete,
            };

            // Generate the shared access signature on the container,
            // setting the constraints directly on the signature.
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the SAS Token
            // Concatenate it on the URI to the resource to get what you need to access the resource.
            return sasContainerToken;
        }

        /// <summary>
        /// Add devices to the hub; specify how many. This creates a number of devices
        ///   with partially random hub names. 
        /// This is a good way to test the import -- create a bunch of devices on one hub,
        ///    then use this the Copy feature to copy the devices to another hub.
        /// Number of devices to create and add. Default is 10.
        /// </summary>
        public async Task AddNewDevices(string hubConnectionString, int NumToAdd)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Create {NumToAdd} new devices for the hub.");

            await GenerateAndAddDevices(hubConnectionString, _containerUriWithSas, NumToAdd, _deviceListFile).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"AddDevices, time elapsed = {stopwatch.Elapsed}.");
            Debug.Print($"AddDevices, time elapsed = {stopwatch.Elapsed}");
        }


        /// <summary>
        ///  This reads the device list from the IoT Hub, then writes them to a file in blob storage. 
        /// </summary>
        public async Task ExportToBlobStorage(string hubConnectionString)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Export devices to blob storage.");

            await ExportDevices(_containerUriWithSas, hubConnectionString).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"Export devices to blob storage: time elapsed = {stopwatch.Elapsed}");
            Debug.Print($"Export devices to blob storage: time elapsed = {stopwatch.Elapsed}");
        }

        /// <summary>
        /// Delete all of the devices from the hub with the given connection string.
        /// </summary>
        /// <param name="hubConnectionString">Connection to the hub from which you want to delete the devices.</param>
        public async Task DeleteFromHub(string hubConnectionString)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Debug.Print("Delete all devices from a hub.");
            Console.WriteLine("Delete all devices from a hub.");
            await DeleteAllDevicesFromHub(hubConnectionString, _containerUriWithSas, _deviceListFile).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"DeleteFromHub: time elapsed = {stopwatch.Elapsed}");
            Debug.Print($"DeleteFromHub: time elapsed = {stopwatch.Elapsed}");
        }


        /// <summary>
        /// Copy the devices from one hub to another. 
        /// </summary>
        /// <param name="prevHubConnectionString">Connection string for source hub.</param>
        /// <param name="newHubConnectionString">Connection string for destination hub.</param>
        public async Task CopyToNewHub(string prevHubConnectionString, string newHubConnectionString)
        {
            Console.WriteLine("Copy devices from the original hub to a new hub.");
            Stopwatch stopwatch = Stopwatch.StartNew();

            await CopyAllDevicesToNewHub(
                prevHubConnectionString,
                newHubConnectionString,
                _containerUriWithSas,
                _deviceListFile).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"CopyToNewHub: time elapsed = {stopwatch.Elapsed}");
            Debug.Print("CopyToNewHub: time elapsed = {stopwatch.Elapsed.ToString()}");
        }

        /// <summary>
        /// Generate NumToAdd devices and add them to the hub.
        /// To do this, generate each identity.
        /// Include authentication keys.
        /// Write the device info to a block blob.
        /// Import the devices into the identity registry by calling the import job.
        /// </summary>
        private async Task GenerateAndAddDevices(string hubConnectionString, string containerURI, int NumToAdd, string devicesToAdd)
        {
            int interimProgressCount = 0;
            int displayProgressCount = 1000;
            int totalProgressCount = 0;

            // generate reference for list of new devices we're going to add, will write list to this blob
            CloudBlockBlob generatedListBlob = _cloudBlobContainer.GetBlockBlobReference(devicesToAdd);

            // define serializedDevices as a generic list<string>
            List<string> serializedDevices = new List<string>(NumToAdd);

            for (var i = 1; i <= NumToAdd; i++)
            {
                // Create device name with this format: Hub_00000000 + a new guid.
                // This should be large enough to display the largest number (1 million).
                string deviceName = $"Hub_{i:D8}_{Guid.NewGuid()}";
                Debug.Print($"device = '{deviceName}'\n");

                // Create a new ExportImportDevice.
                // CryptoKeyGenerator is in the Microsoft.Azure.Devices.Common namespace.
                var deviceToAdd = new ExportImportDevice
                {
                    Id = deviceName,
                    Status = DeviceStatus.Enabled,
                    Authentication = new AuthenticationMechanism
                    {
                        SymmetricKey = new SymmetricKey
                        {
                            PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                            SecondaryKey = CryptoKeyGenerator.GenerateKey(32),
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
                    Console.WriteLine("Added {0} messages.", totalProgressCount);
                    interimProgressCount = 0;
                }
            }

            // Now have a list of devices to be added, each one has been serialized.
            // Write the list to the blob.
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice => sb.AppendLine(serializedDevice));

            // Before writing the new file, make sure there's not already one there.
            await generatedListBlob.DeleteIfExistsAsync().ConfigureAwait(false);

            // Write list of serialized objects to the blob.
            using CloudBlobStream stream = await generatedListBlob.OpenWriteAsync().ConfigureAwait(false);
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            for (var i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes, i, length).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Creating and running registry manager job to write the new devices.");

            // Should now have a file with all the new devices in it as serialized objects in blob storage.
            // generatedListBlob has the list of devices to be added as serialized objects.
            // Call import using the blob to add the new devices.
            // Log information related to the job is written to the same container.
            // This normally takes 1 minute per 100 devices (according to the docs).

            // First, initiate an import job.
            // This reads in the rows from the text file and writes them to IoT Devices.
            // If you want to add devices from a file, you can create a file and use this to import it.
            //   They have to be in the exact right format.
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            try
            {
                // The first URL is the container to import from; the file must be called devices.txt
                // The second URL points to the container to write errors to as a block blob.
                // This lets you import the devices from any file name. Since we wrote the new
                // devices to [devicesToAdd], need to read the list from there as well.
                JobProperties importJob = await registryManager
                    .ImportDevicesAsync(containerURI, containerURI, devicesToAdd)
                    .ConfigureAwait(false);

                // This will catch any errors if something bad happens to interrupt the job.
                while (true)
                {
                    importJob = await registryManager.GetJobAsync(importJob.JobId).ConfigureAwait(false);
                    if (importJob.Status == JobStatus.Completed
                        || importJob.Status == JobStatus.Failed
                        || importJob.Status == JobStatus.Cancelled)
                    {
                        // Job has finished executing
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("exception message {0}", ex.Message);
            }
        }

        /// Get the list of devices registered to the IoT Hub 
        ///   and export it to a blob as deserialized objects.
        private async Task ExportDevices(string containerURI, string hubConnectionString)
        {
            try
            {
                Console.WriteLine("Creating and running registry manager job to retrieve the devices from the hub.");
                // Create an instance of the registry manager class.
                using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

                // Call an export job on the IoT Hub to retrieve all devices.
                // This writes them to devices.txt in the container.
                JobProperties exportJob = await registryManager
                    .ExportDevicesAsync(containerURI, excludeKeys: false)
                    .ConfigureAwait(false);

                // Poll every 5 seconds to see if the job has finished executing.
                while (true)
                {
                    exportJob = await registryManager.GetJobAsync(exportJob.JobId).ConfigureAwait(false);
                    if (exportJob.Status == JobStatus.Completed ||
                        exportJob.Status == JobStatus.Failed ||
                        exportJob.Status == JobStatus.Cancelled)
                    {
                        // Job has finished executing
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Error exporting devices to blob storage. Exception message = {0}", ex.Message);
            }
        }

        // This shows how to delete all of the devices for the IoT Hub.
        // First, export the list to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object;
        // read them into the generic list serializedDevices.
        // Delete the devices.txt in blob storage, because we're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to Delete, 
        // reserialize it, and write it to a StringBuilder. The ImportMode field is what
        // tells the job framework to delete each one.
        // Write the new StringBuilder to the block blob.
        // This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then delete each one. 
        private async Task DeleteAllDevicesFromHub(string hubConnectionString, string containerUri, string deviceListFile)
        {
            Console.WriteLine("Get list of devices from IoT Hub, export to blob storage.");

            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevices(containerUri, hubConnectionString).ConfigureAwait(false);

            // Read devices.txt which contains serialized objects. 
            // Write each line to the serializedDevices list. (List<string>). 
            CloudBlockBlob blockBlob = _cloudBlobContainer.GetBlockBlobReference(deviceListFile);

            // Get the URI for the blob.
            string blobURI = blockBlob.Uri.ToString();

            // Instantiate the generic list.
            var serializedDevices = new List<string>();

            Console.WriteLine("Read list of devices in from blob storage.");

            // Read the blob file of devices, import each row into serializedDevices.
            using Stream blobStream = await blockBlob
                .OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), null, null)
                .ConfigureAwait(false);
            using var streamReader = new StreamReader(blobStream, Encoding.UTF8);
            while (streamReader.Peek() != -1)
            {
                string line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                serializedDevices.Add(line);
            }

            // Delete the blob containing the list of devices, because we're going to recreate it. 
            CloudBlockBlob blobToDelete = _cloudBlobContainer.GetBlockBlobReference("devices.txt");

            Console.WriteLine("Update ImportMode to be 'Delete' for each device, write out to new file.");

            // Step 1: Update each device's ImportMode to be Delete
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice and change import mode.
                var device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                device.ImportMode = ImportMode.Delete;

                // Reserialize the object now that we've updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Delete the blob if it already exists, then write the list in memory to the blob.
            await blobToDelete.DeleteIfExistsAsync().ConfigureAwait(false);
            using CloudBlobStream stream = await blobToDelete.OpenWriteAsync().ConfigureAwait(false);
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            for (var i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes, i, length).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Creating and running registry manager job to delete the devices from the hub.");

            // Step 3: Call import using the same blob to delete all devices.
            // Loads devices.txt and applies that change.
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            JobProperties importJob = await registryManager
                .ImportDevicesAsync(containerUri, containerUri)
                .ConfigureAwait(false);

            // Wait until job is finished
            while (true)
            {
                importJob = await registryManager.GetJobAsync(importJob.JobId).ConfigureAwait(false);
                Console.WriteLine($"Import job status is {importJob.Status}");
                if (importJob.Status == JobStatus.Completed
                    || importJob.Status == JobStatus.Failed
                    || importJob.Status == JobStatus.Cancelled)
                {
                    // Job has finished executing
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        // This shows how to copy devices from one IoT Hub to another.
        // First, export the list from the Source hut to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object; 
        //   read them into the generic list serializedDevices. 
        // Delete the devices.txt in blob storage, because we're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to CREATE, 
        //   reserialize it, and write it to a StringBuilder. The ImportMode field is what
        //   tells the job framework to add each device.
        // Write the new StringBuilder to the block blob.
        //   This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then add each one 
        //   because it doesn't already exist. If it already exists, it will write an entry to
        //   the import error log and not add the new one.
        private async Task CopyAllDevicesToNewHub(string sourceHubConnectionString, string destHubConnectionString, string containerUri, string deviceListFile)
        {
            Console.WriteLine("Exporting devices on current hub");

            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevices(containerUri, sourceHubConnectionString).ConfigureAwait(false);

            // Read devices.txt which contains serialized objects. 
            // Write each line to the serializedDevices list. (List<string>). 
            CloudBlockBlob blockBlob = _cloudBlobContainer.GetBlockBlobReference(deviceListFile);

            // Get the URI for the blob.
            string blobUri = blockBlob.Uri.ToString();

            // Instantiate the generic list.
            var serializedDevices = new List<string>();

            Console.WriteLine("Read in list of devices from blob storage.");

            // Read the blob file of devices, import each row into serializedDevices.
            using Stream blobStream = await blockBlob.OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), null, null).ConfigureAwait(false);
            using var streamReader = new StreamReader(blobStream, Encoding.UTF8);
            while (streamReader.Peek() != -1)
            {
                string line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                serializedDevices.Add(line);
            }

            // Delete the blob containing the list of devices, because we're going to recreate it.
            CloudBlockBlob blobToDelete = _cloudBlobContainer.GetBlockBlobReference("devices.txt");

            Console.WriteLine("Update ImportMode to be Create.");

            // Step 1: Update each device's ImportMode to Create
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice and update the import mode property.
                ExportImportDevice device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                device.ImportMode = ImportMode.Create;

                // Reserialize the object now that we've updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Delete the blob if it already exists, then write the in-memory list to the blob.
            await blobToDelete.DeleteIfExistsAsync().ConfigureAwait(false);

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            using CloudBlobStream stream = await blobToDelete.OpenWriteAsync().ConfigureAwait(false);
            for (var i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes, i, length).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Creating and running registry manager job to import the entries from the text file to the new hub");

            // Step 3: Call import using the same blob to create all devices.
            // Loads devices.txt and adds the devices to the destination hub.
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(destHubConnectionString);
            JobProperties importJob = await registryManager.ImportDevicesAsync(containerUri, containerUri).ConfigureAwait(false);

            // Wait until job is finished
            while (true)
            {
                importJob = await registryManager.GetJobAsync(importJob.JobId).ConfigureAwait(false);
                Console.WriteLine($"Import job status is {importJob.Status}");
                if (importJob.Status == JobStatus.Completed
                    || importJob.Status == JobStatus.Failed
                    || importJob.Status == JobStatus.Cancelled)
                {
                    // Job has finished executing
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }
    }
}
