using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using System;

namespace ImportExportDevices
{
    public class Program
    {

        //IoT Hub connection string. You can get this from the portal.
        // Log into https://azure.portal.com, go to Resources, find your hub and select it.
        // Then look for Shared Access Policies and select it. 
        // Then select IoThubowner and copy one of the connection strings.
        public static string IoTHubConnectionString = "<connection string to an existing IoT hub>";

        // When copying data from one hub to another, this is the connection string
        //   to the destination hub, i.e. the new one.
        public static string DestIoTHubConnectionString = "<connection string to a IoT hub to copy devices to>";

        // Connection string to the storage account used to hold the imported or exported data.
        // Log into https://azure.portal.com, go to Resources, find your storage account and select it.
        // Select Access Keys and copy one of the connection strings.
        public static string storageAccountConnectionString = "<your storage account connection string>";

        // Container used to hold the blob containing the list of import/export files.
        // This is a module-wide variable. If this project doesn't find this container, it will create it.
        public static CloudBlobContainer cloudBlobContainer;

        // Name of blob container holding the work data.
        public static string containerName = "devicefiles";

        // Name of the file used for exports and imports. 
        // This is set by the IoT SDK, and can't be changed.
        public static string deviceListFile = "devices.txt";

        public static void Main(string[] args)
        {
            //To use this sample, uncomment the bits you want to run.

            //The size of the hub you are using should be able to manage the number of devices 
            //  you want to create and test with.
            //For example, if you want to create a million devices, don't use a hub with a Basic sku!

            // You must run this; it creates the blob storage resource.
            string containerURI = PrepareStorageForImportExport();

            //Console.WriteLine("Create new devices for the hub.");
            // Add devices to the hub; specify how many. This creates a number of devices
            //   with partially random hub names. 
            // This is a good way to test the import -- create a bunch of devices on one hub,
            //    then use this the Copy feature to copy the devices to another hub.
            // Number of devices to create and add. Default is 10.
            //int NumToAdd =  5; 
            //IoTHubDevices.GenerateAndAddDevices(IoTHubConnectionString, cloudBlobContainer, 
            //   containerURI, NumToAdd, deviceListFile).Wait();

            //  This exports the devices to a file in blob storag.e 
            //  You can use this to add a bunch of new devices, then export them and look at them in a file (in blob storage).
            //Console.WriteLine("Read devices from the original hub, write to blob storage.");
            // Read the list of registered devices for the IoT Hub.
            // Write them to blob storage.
            //IoTHubDevices.ExportDevices(containerURI, IoTHubConnectionString).Wait();

            Console.WriteLine("Copy devices from the original hub to a new hub.");
            // Copy devices from an existing hub to a new hub.
            IoTHubDevices.CopyAllDevicesToNewHub(IoTHubConnectionString, DestIoTHubConnectionString, 
                cloudBlobContainer, containerURI, deviceListFile).Wait();

            //** uncomment this if you want to delete all the devices registered to a hub **

            // Delete devices from the source hub.
            //Console.WriteLine("Delete all devices from the source hub.");
            //IoTHubDevices.DeleteAllDevicesFromHub(IoTHubConnectionString, cloudBlobContainer, containerURI, deviceListFile).Wait();

            // Delete devices from the destination hub.
            //Console.WriteLine("Delete all devices from the destination hub.");
            //IoTHubDevices.DeleteAllDevicesFromHub(DestIoTHubConnectionString, cloudBlobContainer, containerURI, deviceListFile).Wait();

            Console.WriteLine("Finished.");
            Console.WriteLine();
            Console.Write("Press any key to continue.");
            Console.ReadLine();

        }

        /// <summary>
        /// Sets up references to the blob hierarchy objects, sets containerURI with an SAS for access.
        /// Create the container if it doesn't exist.
        /// </summary>
        /// <returns>URI to blob container, including SAS token</returns>
        private static string PrepareStorageForImportExport()
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
