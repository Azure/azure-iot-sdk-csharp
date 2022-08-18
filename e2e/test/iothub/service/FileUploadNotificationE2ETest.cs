// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for FileUploadNotification.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadNotificationE2eTest : E2EMsTestBase
    {
        public bool fileUploaded;

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task FileUploadNotification_Operation()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = fileUploadCallback;
            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);
            fileUploaded = false;
            await UploadFile().ConfigureAwait(false);
            var timer = Stopwatch.StartNew();
            while (!fileUploaded && timer.ElapsedMilliseconds < 10000)
            {
                continue;
            }
            timer.Stop();
            if (!fileUploaded)
                throw new AssertionFailedException("Timed out waiting to receive file upload notification.");

            await serviceClient.FileUploadNotificationProcessor.CloseAsync();
            fileUploaded.Should().BeTrue();
        }

        private AcknowledgementType fileUploadCallback(FileUploadNotification notification)
        {
            fileUploaded = true;
            return AcknowledgementType.Complete;
        }

        private async Task UploadFile()
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
            FileUploadSasUriResponse sasUri = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest);
            Uri uploadUri = sasUri.GetBlobUri();

            var blob = new CloudBlockBlob(uploadUri);
            Task uploadTask = blob.UploadFromStreamAsync(fileStreamSource);
            await uploadTask.ConfigureAwait(false);

            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
            {
                CorrelationId = sasUri.CorrelationId,
                IsSuccess = true,
                StatusCode = 200,
                StatusDescription = "Success"
            };

            await deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification);
        }
    }
}
