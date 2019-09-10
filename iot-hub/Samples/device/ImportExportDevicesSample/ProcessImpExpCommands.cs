using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImportExportDevices
{
    public static class IoTHubDevices
    {

        //IoT Hub connection string. You can get this from the portal.
        // Log into https://azure.portal.com, go to Resources, find your hub and select it.
        // Then look for Shared Access Policies and select it. 
        // Then select IoThubowner and copy one of the connection strings.
        public static string IoTHubConnectionString = "<connection string to your IoT hub>";

        // Connection string to the storage account used to hold the imported or exported data.
        // Log into https://azure.portal.com, go to Resources, find your storage account and select it.
        // Select Access Keys and copy one of the connection strings.
        public static string storageAccountConnectionString = "<your storage account connection string>";

        // Container used to hold the blob containing the list of import/export files.
        // This is a module-wide variable.
        public static CloudBlobContainer cloudBlobContainer;

        // Name of blob container holding the work data.
        public static string containerName = "devicefiles";

        // Name of the file used for exports and imports. 
        // This is set by the IoT SDK, and can't be changed.
        public static string deviceListFile = "devices.txt";

        // List of devices to add. This is a temporary file.
        public static string devicesToAdd = "devices_new.txt";

        /// <summary>
        /// Sets up references to the blob hierarchy objects, sets containerURI with an SAS for access.
        /// Create the container if it doesn't exist.
        /// </summary>
        /// <returns></returns>
        public static string PrepareForImportExport()
        {
            // Get reference to storage account.
            // This is the storage account used to hold the import and export files.
            CloudStorageAccount cloudStorageAccount =
                 CloudStorageAccount.Parse(storageAccountConnectionString);

            // Get reference to the blob client.
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            // Get reference to the container to be used.
            cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

            // Get the URI to the container. This doesn't have an SAS token (yet).
            string containerURI = cloudBlobContainer.Uri.ToString();

            // How to get reference to a blob. 
            //   Just leaving this in here in case you want to copy this block of code and use it for this.
            // CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(deviceListFile);
            // This is how you get the URI to the blob. The import and export both use a container
            //   with container-level access, so this isn't used for importing or exporting the devices.
            // Just leaving it in here for when it's needed.
            // string blobURI = blockBlob.Uri.ToString();

            try
            {
                // The call below will fail if the sample is configured to use the storage emulator 
                //   in the connection string but the emulator is not running.
                // Change the retry policy for this call so that if it fails, it fails quickly.
                BlobRequestOptions requestOptions = new BlobRequestOptions() { RetryPolicy = new NoRetry() };
                cloudBlobContainer.CreateIfNotExistsAsync(requestOptions, null);
            }
            catch (StorageException)
            {
                throw;
            }

            // Call to get the SAS token for container-level access.
            string containerSASToken = GetContainerSasToken(cloudBlobContainer);

            // Append the SAS token to the URI to the container. This is returned.
            return containerURI + containerSASToken;
        }

        /// <summary>
        /// Add NumToAdd devices to the IoT Hub. The limit is 1 million. 
        /// You should size your hub so it can have as many devices as you want to test with.
        /// </summary>
        /// <param name="NumToAdd">Number of devices to add.</param>
        public async static Task AddDevicesToHub(int NumToAdd)
        { 
            string containerURI = PrepareForImportExport();
            // Add NumToAdd devices to the IoT Hub.
            // This won't change any devices already registered with the hub, it just adds more.
            // The default is 10. 
            if (NumToAdd < 0)
            {
                NumToAdd = 10;
            }        
            await IoTHubDevices.GenerateAndAddDevices(containerURI, NumToAdd);
        }

        /// <summary>
        /// Read the list of registered devices from the IoT hub and export it to blob storage.
        /// </summary>
        public async static Task ExportDevicesToBlobStorage()
        {
            string containerURI = PrepareForImportExport();
            await IoTHubDevices.ExportDevices(containerURI);
        }

        /// <summary>
        /// Read the device list that was exported to blob storage,
        ///   then print it to the console.
        /// The point of this is just so you can see the data.
        /// </summary>
        /// <returns></returns>
        public async static Task ReadAndDisplayExportedDeviceList()
        {
            string containerURI = PrepareForImportExport();
            // First, export IoT device list to the blob.
            await IoTHubDevices.ExportDevices(containerURI);
            // Next, read the data in the blob and show it on the screen.
            await IoTHubDevices.ReadExportedDeviceList();
        }

        /// <summary>
        /// Read a list of devices from IoT Hub, then delete them. 
        /// This version of this deletes all devices registered to the hub. 
        /// </summary>
        /// <returns></returns>
        public async static Task DeleteAllDevicesFromHub()
        {
            string containerURI = PrepareForImportExport();
            await IoTHubDevices.DeleteDevices(containerURI);
        }

        //generate NumToAdd devices and add them to the hub 
        // to do this, generate each identity 
        // * include authentication keys
        // * write the device info to a block blob
        // * import the devices into the identity registry by calling the import job
        private static async Task GenerateAndAddDevices(string containerURI, int NumToAdd)
        {
            //generate reference for list of new devices you're going to add, will write list to this blob 
            CloudBlockBlob generatedListBlob = cloudBlobContainer.GetBlockBlobReference(devicesToAdd);

            // define serializedDevices as a generic list<string>
            List<string> serializedDevices = new List<string>();
            
            for (var i = 1; i <= NumToAdd; i++)
            {
                // Create device name with this format: Hub_00000000 + a new guid.
                // This should be large enough to display the largest number (1 million).
                //string deviceName = "Hub_" + i.ToString("D8") + "-" + Guid.NewGuid().ToString();
                string deviceName = $"Hub_{i.ToString("D8")}-{Guid.NewGuid().ToString()}";

                System.Diagnostics.Debug.Print("device = '{0}'", deviceName);
                // Create a new ExportImportDevice.
                // CryptoKeyGenerator is in the Microsoft.Azure.Devices.Common namespace.
                var deviceToAdd = new ExportImportDevice()
                {
                    Id = deviceName,
                    Status = DeviceStatus.Enabled,
                    Authentication = new AuthenticationMechanism()
                    {
                        SymmetricKey = new SymmetricKey()
                        {
                            PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                            SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                        }
                    },
                    // This indicates that the entry should be added as a new device.
                    ImportMode = ImportMode.Create
                };

                // Add device to the list as a serialized object.
                serializedDevices.Add(JsonConvert.SerializeObject(deviceToAdd));
            }

            // Now have a list of devices to be added, each one has been serialized.
            // Write the list to the blob.
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice => sb.AppendLine(serializedDevice));

            // Before writing the new file, make sure there's not already one there.
            await generatedListBlob.DeleteIfExistsAsync();

            // Write list of serialized objects to the blob.
            using (CloudBlobStream stream = await generatedListBlob.OpenWriteAsync())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                for (var i = 0; i < bytes.Length; i += 500)
                {
                    int length = Math.Min(bytes.Length - i, 500);
                    await stream.WriteAsync(bytes, i, length);
                }
            }

            // Should now have a file with all the new devices in it as serialized objects in blob storage.

            // generatedListBlob has the list of devices to be added as serialized objects.

            // Call import using the blob to add the new devices. 
            // Log information related to the job is written to the same container.
            // This normally takes 1 minute per 100 devices.

            // First, initiate an import job.
            // This reads in the rows from the text file and writes them to IoT Devices.

            JobProperties importJob = new JobProperties();
            RegistryManager registryManager =
                RegistryManager.CreateFromConnectionString(IoTHubConnectionString);
            try
            {
                // First URL is the container to import from; the file must be called devices.txt
                // Second URL points to the container to write errors to as a block blob.
                // This lets you import the devices from any file name. Since we wrote the new 
                //   devices to [devicesToAdd], need to read the list from there as well. 
                importJob =
                  await registryManager.ImportDevicesAsync(containerURI, containerURI, devicesToAdd);

                // This will catch any errors if something bad happens to interrupt the job.
                while (true)
                {
                    importJob = await registryManager.GetJobAsync(importJob.JobId);
                    if (importJob.Status == JobStatus.Completed ||
                        importJob.Status == JobStatus.Failed ||
                        importJob.Status == JobStatus.Cancelled)
                    {
                        // Job has finished executing
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.Print("description = {0}", ex.Message);
            }

        }

        /// Get the list of devices registered to the IoT Hub 
        ///   and export it to a blob as deserialized objects.
        /// Can use this for testing -- read the devices and export them, 
        ///   then open the file and see what it has in it.
        private static async Task ExportDevices(string containerURI)
        {
            // Create an instance of the registry manager class.
            RegistryManager registryManager =
              RegistryManager.CreateFromConnectionString(IoTHubConnectionString);

            // Call an export job on the IoT Hub to retrieve all devices.
            // This writes them to devices.txt in the container. 
            // The second parameter indicates whether to export the keys or not.
            JobProperties exportJob = await
              registryManager.ExportDevicesAsync(containerURI, false);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                exportJob = await registryManager.GetJobAsync(exportJob.JobId);
                if (exportJob.Status == JobStatus.Completed ||
                    exportJob.Status == JobStatus.Failed ||
                    exportJob.Status == JobStatus.Cancelled)
                {
                    // Job has finished executing
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        
            // Note: could add twin data here if you want to export it.
        }

        /// <summary>
        /// Read the list of devices from the existing blob.
        /// Deserialize each entry and add it to exportedDevices.
        /// Then show the list in the output window.
        /// This is for testing.
        /// </summary>
        /// <returns></returns>
        private static async Task ReadExportedDeviceList()
        {
            // Get reference to the location of the blob that contains the device list from the IoT Hub.
            CloudBlockBlob exportedListBlob = cloudBlobContainer.GetBlockBlobReference(deviceListFile);

            // Instantiate a list of devices to read the blob into.
            List<ExportImportDevice> exportedDevices = new List<ExportImportDevice>();

            // Read the blob. Deserialize each row into an object and write it to exportedDevices.
            // You end up with a list of devices that exist for the hub in a list in memory.
            using (var streamReader = new StreamReader(await exportedListBlob.OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), null, null), Encoding.UTF8))
            {
                while (streamReader.Peek() != -1)
                {
                    string line = await streamReader.ReadLineAsync();
                    var device = JsonConvert.DeserializeObject<ExportImportDevice>(line);
                    exportedDevices.Add(device);
                }
            }

            // Print out the list of devices to the console.
            foreach (ExportImportDevice exportImportDevice in exportedDevices)
            {
                System.Diagnostics.Debug.Print("Hub id = {0}, eTag = {1}", exportImportDevice.Id, exportImportDevice.ETag);
            }
        }

        // This shows how to delete all of the devices for the IoT Hub.
        // First, export the list to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object; 
        //   each object to a list called serializedDevices. (List<string>).
        // For each serializedDevice, deserialize it, set ImportMode to Delete, 
        //   reserialize it, and write it to a StringBuilder. The ImportMode field is what
        //   tells the job framework to delete each one.
        // Write the new StringBuilder to the block blob. 
        //   This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then delete each one. 
        private static async Task DeleteDevices(string containerURI)
        {
            // Read the devices from the hub and write= them to devices.txt.
            await ExportDevices(containerURI);

            // Read devices.txt which contains serialized objects. 
            // Write each line to the serializedDevices list. (List<string>).
            CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(deviceListFile);
            
            // Get the URI for the blob.
            string blobURI = blockBlob.Uri.ToString();

            // Instantiate the generic list.
            var serializedDevices = new List<string>();

            // Read the blob file of devices, import each row into serializedDevices.
            using (var streamReader = 
              new StreamReader(await blockBlob.OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), 
                null, null), Encoding.UTF8))
            {
                while (streamReader.Peek() != -1)
                {
                    string line = await streamReader.ReadLineAsync();
                    serializedDevices.Add(line);
                }
            }

            // Delete the blob containing the list of devices,
            //   because you're going to recreate it. 
            CloudBlockBlob blobToDelete = cloudBlobContainer.GetBlockBlobReference("devices.txt");

            // Step 1: Update each device's ImportMode to be Delete
            StringBuilder sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice.
                var device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);

                // Update the property.
                device.ImportMode = ImportMode.Delete;

                // Re-serialize the object now that you're updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Delete the blob if it already exists, then write the list in memory to the blob.
            
            await blobToDelete.DeleteIfExistsAsync();
            using (CloudBlobStream stream = await blobToDelete.OpenWriteAsync())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                for (var i = 0; i < bytes.Length; i += 500)
                {
                    int length = Math.Min(bytes.Length - i, 500);
                    await stream.WriteAsync(bytes, i, length);
                }
            }

            // Step 3: Call import using the same blob to delete all devices.
            // Loads devices.txt and applies that change.
            RegistryManager registryManager =
              RegistryManager.CreateFromConnectionString(IoTHubConnectionString);
            JobProperties importJob =
              await registryManager.ImportDevicesAsync(containerURI, containerURI);

            // Wait until job is finished
            while (true)
            {
                importJob = await registryManager.GetJobAsync(importJob.JobId);
                if (importJob.Status == JobStatus.Completed ||
                    importJob.Status == JobStatus.Failed ||
                    importJob.Status == JobStatus.Cancelled)
                {
                    // Job has finished executing
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// Create the SAS token for the container. 
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private static string GetContainerSasToken(CloudBlobContainer container)
        {
            // Set the expiry time and permissions for the container.
            // In this case no start time is specified, so the
            // shared access signature becomes valid immediately.
            var sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasConstraints.Permissions =
              SharedAccessBlobPermissions.Write |
              SharedAccessBlobPermissions.Read |
              SharedAccessBlobPermissions.Delete;

            // Generate the shared access signature on the container,
            // setting the constraints directly on the signature.
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the SAS Token
            // Concatenate it on the URI to the resource to get what you need to access the resource.
            return sasContainerToken;
        }


    }

}
