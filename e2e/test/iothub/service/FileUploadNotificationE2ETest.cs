using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for FileUploadNotification.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadNotificationE2ETest : E2EMsTestBase
    {
        public bool fileUploaded;
        [TestMethod]
        public async Task FileUploadNotification_Operation()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            serviceClient.FileUploadNotificationProcessor._fileNotificationProcessor = fileUploadCallback;
            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);

            await uploadFile().ConfigureAwait(false);
            Thread.Sleep(10000);

            await serviceClient.FileUploadNotificationProcessor.CloseAsync();
            Assert.IsTrue(fileUploaded);
        }

        private DeliveryAcknowledgement fileUploadCallback(FileNotification notification)
        {
            fileUploaded = true;
            return DeliveryAcknowledgement.PositiveOnly;
        }

        private async Task uploadFile()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString($"{TestConfiguration.IoTHub.ConnectionString};DeviceId={TestConfiguration.IoTHub.X509ChainDeviceName}");
            const string filePath = "TestPayload.txt";
            using FileStream fileStreamSource = File.Create(filePath);
            using var sr = new StreamWriter(fileStreamSource);

            sr.WriteLine("TestPayload");
            string fileName = Path.GetFileName(fileStreamSource.Name);

            Console.WriteLine($"Uploading file {fileName}");

            var fileUploadTime = Stopwatch.StartNew();

            var fileUploadSasUriRequest = new FileUploadSasUriRequest
            {
                BlobName = fileName
            };

            Console.WriteLine("Getting SAS URI from IoT Hub to use when uploading the file...");
            try
            {
                FileUploadSasUriResponse sasUri = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest);
                Uri uploadUri = sasUri.GetBlobUri();
                var blockBlobClient = new BlockBlobClient(uploadUri);
                await blockBlobClient.UploadAsync(fileStreamSource, new BlobUploadOptions());
                var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
                {
                    CorrelationId = sasUri.CorrelationId,
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusDescription = "Success1"
                };

                await deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }
    }
}
