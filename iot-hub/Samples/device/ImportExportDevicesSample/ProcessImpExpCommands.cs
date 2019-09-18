using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
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

        //generate NumToAdd devices and add them to the hub 
        // to do this, generate each identity 
        // * include authentication keys
        // * write the device info to a block blob
        // * import the devices into the identity registry by calling the import job
        public static async Task GenerateAndAddDevices(string hubConnectionString, 
            CloudBlobContainer cloudBlobContainer,
            string containerURI, int NumToAdd, string devicesToAdd)
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
//**USE THIS TO ADD FROM FILE**
            JobProperties importJob = new JobProperties();
            RegistryManager registryManager =
                RegistryManager.CreateFromConnectionString(hubConnectionString);
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
        public static async Task ExportDevices(string containerURI, string hubConnectionString)
        {
            // Create an instance of the registry manager class.
            RegistryManager registryManager =
              RegistryManager.CreateFromConnectionString(hubConnectionString);

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

        // This shows how to delete all of the devices for the IoT Hub.
        // First, export the list to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object; 
        //   read them into the generic list serializedDevices. 
        // Delete the devices.txt in blob storage, because you're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to Delete, 
        //   reserialize it, and write it to a StringBuilder. The ImportMode field is what
        //   tells the job framework to delete each one.
        // Write the new StringBuilder to the block blob.
        //   This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then delete each one. 
        public static async Task DeleteAllDevicesFromHub(string hubConnectionString, 
            CloudBlobContainer cloudBlobContainer,string containerURI, string deviceListFile)
        {
            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevices(containerURI, hubConnectionString);

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
              RegistryManager.CreateFromConnectionString(hubConnectionString);
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

        //---------------------------------------------------------------------------------------
        // This shows how to copy devices from one IoT Hub to another.
        // First, export the list from the Source hut to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object; 
        //   read them into the generic list serializedDevices. 
        // Delete the devices.txt in blob storage, because you're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to CREATE, 
        //   reserialize it, and write it to a StringBuilder. The ImportMode field is what
        //   tells the job framework to add each device.
        // Write the new StringBuilder to the block blob.
        //   This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then add each one 
        //   because it doesn't already exist. If it already exists, it will write an entry to
        //   the import error log and not add the new one.
        public static async Task CopyAllDevicesToNewHub(string sourceHubConnectionString,
            string destHubConnectionString, CloudBlobContainer cloudBlobContainer, 
            string containerURI, string deviceListFile)        {

            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevices(containerURI, sourceHubConnectionString);

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

            // Step 1: Update each device's ImportMode to be Create
            StringBuilder sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice.
                var device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);

                // Update the property.
                device.ImportMode = ImportMode.Create;

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

            // Step 3: Call import using the same blob to create all devices.
            // Loads devices.txt and adds the devices to the destination hub.
            RegistryManager registryManager =
              RegistryManager.CreateFromConnectionString(destHubConnectionString);
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



    }

}
