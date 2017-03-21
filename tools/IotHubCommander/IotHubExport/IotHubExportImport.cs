using Microsoft.Azure.Devices;
using Microsoft.Framework.Configuration.CommandLine;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHubCommander.IotHubExport
{
    public class IotHubExportImport
    {
        private string m_ContainerName;
        private string m_AccountName;
        private string m_Key;
        private string m_FromIotHubConnStr;
        private string m_ToIotHubConnStr;


        public IotHubExportImport(CommandLineConfigurationProvider cmdConfig)
        {
            this.m_FromIotHubConnStr = cmdConfig.GetArgument("migrateiothub");
            this.m_ToIotHubConnStr = cmdConfig.GetArgument("to");
            this.m_AccountName = cmdConfig.GetArgument("acc");
            this.m_Key = cmdConfig.GetArgument("key");
        }

        public IotHubExportImport(string accountName,
            string key, string fromIotHubConnStr, string toIotHubConnStr)
        {
            this.m_AccountName = accountName;
            this.m_Key = key;
            this.m_FromIotHubConnStr = fromIotHubConnStr;
            this.m_ToIotHubConnStr = toIotHubConnStr;
        }

        public void Run()
        {
            CloudBlobContainer container = createContainer("output", m_AccountName, m_Key);

            string sasUriOut = getContainerSasUri(container);

            export(this.m_FromIotHubConnStr, sasUriOut).Wait();

            container = createContainer("err", m_AccountName, m_Key);

            string sasUriErr = getContainerSasUri(container);

            import(this.m_ToIotHubConnStr, sasUriOut, sasUriErr).Wait();
        }

        private static CloudBlobContainer createContainer(string containerName, string accountName, string key)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, key), true);

            //Create the blob client object.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Get a reference to a container to use for the sample code, and create it if it does not exist.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();

            return container;
        }

        private static string getContainerSasUri(CloudBlobContainer container)
        {
            //Set the expiry time and permissions for the container.
            //In this case no start time is specified, so the shared access signature becomes valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Delete;

            //Generate the shared access signature on the container, setting the constraints directly on the signature.
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            //Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }

        private static async Task export(string connStrFrom, string sasUri)
        {
            Console.WriteLine("Started exporting...");
            RegistryManager regMgr = RegistryManager.CreateFromConnectionString(connStrFrom);

            var jobs = regMgr.GetJobsAsync().Result;
            foreach (var item in jobs)
            {
                try
                {
                    Debug.WriteLine(item.Status);

                    if(item.Status == JobStatus.Running )
                    regMgr.CancelJobAsync(item.JobId).Wait();
                }
                catch (Exception ex)
                {
                }
            }

            var exportJob = await regMgr.ExportDevicesAsync(sasUri, false);

            while (true)
            {
                if (exportJob.Status == JobStatus.Completed ||
                    exportJob.Status == JobStatus.Failed ||
                    exportJob.Status == JobStatus.Cancelled)
                {
                    if (exportJob.Status != JobStatus.Completed)
                        throw new Exception($"Export failed with status: {exportJob.Status}");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Console.WriteLine("Exporting completed.");
        }

        private static async Task import(string connStrTo, string sasUri, string sasUriErr)
        {
            Console.WriteLine("Started importing...");
            RegistryManager regMgr = RegistryManager.CreateFromConnectionString(connStrTo);

            var importJob = await regMgr.ImportDevicesAsync(sasUri, sasUriErr);

            while (true)
            {
                if (importJob.Status == JobStatus.Completed ||
                    importJob.Status == JobStatus.Failed ||
                    importJob.Status == JobStatus.Cancelled)
                {
                    if (importJob.Status != JobStatus.Completed)
                        throw new Exception($"Import failed with status: {importJob.Status}");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Console.WriteLine("Importing completed.");
        }
    }

}
