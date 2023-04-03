// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for testing receiving file upload notifications.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    public class FileUploadNotificationE2ETest : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(FileUploadNotificationE2ETest)}_";

        // All file upload notifications will be acknowledged with this type. We are deliberately
        // choosing to Abandon rather than Complete because each test process may receive file upload
        // notifications that another test was looking for. By abandoning each received notification,
        // the service makes it available for redelivery to other open receivers.
        private readonly AcknowledgementType _defaultAcknowledgementType = AcknowledgementType.Abandon;

        [TestMethod]
        [DataRow(IotHubTransportProtocol.Tcp, 1, false)]
        [DataRow(IotHubTransportProtocol.Tcp, 2, false)]
        [DataRow(IotHubTransportProtocol.Tcp, 1, true)]
        [DataRow(IotHubTransportProtocol.WebSocket, 1, false)]
        [DataRow(IotHubTransportProtocol.WebSocket, 1, true)]
        public async Task FileUploadNotification_FileUploadNotificationProcessor_ReceivesNotifications(IotHubTransportProtocol protocol, int filesToUpload, bool shouldReconnect)
        {
            // arrange

            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            using StorageContainer storage = await StorageContainer.GetInstanceAsync("fileupload", false).ConfigureAwait(false);
            using var fileNotification = new SemaphoreSlim(1, 1);

            try
            {
                var files = new Dictionary<string, bool>(filesToUpload);
                var allFilesFound = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                async Task<AcknowledgementType> OnFileUploadNotificationReceived(FileUploadNotification fileUploadNotification)
                {
                    string fileName = fileUploadNotification.BlobName.Substring(fileUploadNotification.BlobName.IndexOf('/') + 1);
                    if (!files.ContainsKey(fileName))
                    {
                        // Notification does not belong to this test
                        VerboseTestLogger.WriteLine($"Received notification for unrelated file {fileName}.");
                        return _defaultAcknowledgementType;
                    }

                    VerboseTestLogger.WriteLine($"Received notification for {fileName}.");
                    if (!files[fileName])
                    {
                        files[fileName] = true;
                        CloudBlob blob = storage.CloudBlobContainer.GetBlobReference(fileUploadNotification.BlobName);
                        VerboseTestLogger.WriteLine($"Deleting blob {fileUploadNotification.BlobName}...");
                        await blob.DeleteIfExistsAsync(cts.Token).ConfigureAwait(false);
                    }

                    if (files.All(x => x.Value))
                    {
                        VerboseTestLogger.WriteLine($"Notifications have been received for all files uploads!");
                        allFilesFound.TrySetResult(true);
                    }

                    return AcknowledgementType.Complete;
                }

                serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
                VerboseTestLogger.WriteLine($"Opening client...");
                await serviceClient.FileUploadNotifications.OpenAsync(cts.Token).ConfigureAwait(false);
                if (shouldReconnect)
                {
                    VerboseTestLogger.WriteLine($"Closing client...");
                    await serviceClient.FileUploadNotifications.CloseAsync(cts.Token).ConfigureAwait(false);
                    VerboseTestLogger.WriteLine($"Reopening client...");
                    await serviceClient.FileUploadNotifications.OpenAsync(cts.Token).ConfigureAwait(false);
                }

                // act
                for (int i = 0; i < filesToUpload; ++i)
                {
                    string fileName = $"TestPayload-{Guid.NewGuid()}.txt";
                    files.Add(fileName, false);
                    await UploadFile(fileName, cts.Token).ConfigureAwait(false);
                }

                VerboseTestLogger.WriteLine($"Waiting on file upload notification...");
                await allFilesFound.WaitAsync(cts.Token).ConfigureAwait(false);

                // assert
                allFilesFound.Task.IsCompleted.Should().BeTrue();
            }
            finally
            {
                VerboseTestLogger.WriteLine($"Cleanup: closing client...");
                await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task FileUploadNotification_ErrorProcessor_ReceivesNotifications(IotHubTransportProtocol protocol)
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            try
            {
                var errorProcessorNotified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = (_) => Task.FromResult(_defaultAcknowledgementType);
                serviceClient.FileUploadNotifications.ErrorProcessor = (errorContext) =>
                {
                    VerboseTestLogger.WriteLine("Error processor fired.");
                    errorProcessorNotified.TrySetResult(true);
                    return Task.CompletedTask;
                };

                VerboseTestLogger.WriteLine("Opening client...");
                await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
                VerboseTestLogger.WriteLine("Client opened.");

                VerboseTestLogger.WriteLine("Client closing...");
                await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
                VerboseTestLogger.WriteLine("Client closed.");

                // The open file upload notification processor should be able to receive more than one
                // file upload notification without closing and re-opening as long as there is more
                // than one file upload notification to consume.
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestTimeoutMilliseconds));
                await errorProcessorNotified.WaitAsync(cts.Token).ConfigureAwait(false);
                errorProcessorNotified.Task.IsCompleted.Should().BeTrue();
            }
            finally
            {
                serviceClient.FileUploadNotifications.ErrorProcessor = null;
                await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task UploadFile(string fileName, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("TestPayload"));

            VerboseTestLogger.WriteLine($"Uploading file {fileName}.");
            var fileUploadSasUriRequest = new FileUploadSasUriRequest(fileName);
            FileUploadSasUriResponse sasUri = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest, ct).ConfigureAwait(false);
            Uri uploadUri = sasUri.GetBlobUri();

            var blob = new CloudBlockBlob(uploadUri);
            await blob.UploadFromStreamAsync(ms, ct).ConfigureAwait(false);

            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, true)
            {
                StatusCode = 200,
                StatusDescription = "Success"
            };

            VerboseTestLogger.WriteLine($"Completing upload for {fileName}");
            await deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification, ct).ConfigureAwait(false);
        }
    }
}
