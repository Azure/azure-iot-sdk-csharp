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
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            try
            {
                var files = new Dictionary<string, bool>(filesToUpload);
                var allFilesFound = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<AcknowledgementType> OnFileUploadNotificationReceived(FileUploadNotification fileUploadNotification)
                {
                    string fileName = fileUploadNotification.BlobName.Substring(fileUploadNotification.BlobName.IndexOf('/') + 1);
                    if (!files.ContainsKey(fileName))
                    {
                        // Notification does not belong to this test
                        return Task.FromResult(_defaultAcknowledgementType);
                    }

                    files[fileName] = true;
                    if (files.All(x => x.Value))
                    {
                        allFilesFound.TrySetResult(true);
                    }

                    return Task.FromResult(AcknowledgementType.Complete);
                }

                serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
                await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
                if (shouldReconnect)
                {
                    await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
                    await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
                }

                for (int i = 0; i < filesToUpload; ++i)
                {
                    string fileName = $"TestPayload-{Guid.NewGuid()}.txt";
                    files.Add(fileName, false);
                    await UploadFile(fileName).ConfigureAwait(false);
                }

                // The open file upload notification processor should be able to receive more than one
                // file upload notification without closing and re-opening as long as there is more
                // than one file upload notification to consume.
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestTimeoutMilliseconds));
                await Task
                    .WhenAny(
                        allFilesFound.Task,
                        Task.Delay(-1, cts.Token))
                    .ConfigureAwait(false);
                allFilesFound.Task.IsCompleted.Should().BeTrue();
            }
            finally
            {
                await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task UploadFile(string fileName)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("TestPayload"));

            Console.WriteLine($"Uploading file {fileName}");

            var fileUploadSasUriRequest = new FileUploadSasUriRequest(fileName);
            FileUploadSasUriResponse sasUri = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest).ConfigureAwait(false);
            Uri uploadUri = sasUri.GetBlobUri();

            var blob = new CloudBlockBlob(uploadUri);
            await blob.UploadFromStreamAsync(ms).ConfigureAwait(false);

            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, true)
            {
                StatusCode = 200,
                StatusDescription = "Success"
            };

            await deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification).ConfigureAwait(false);
        }
    }
}
