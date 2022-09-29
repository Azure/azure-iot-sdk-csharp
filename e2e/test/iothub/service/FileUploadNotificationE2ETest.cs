// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
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
    [TestCategory("IoTHub")]
    public class FileUploadNotificationE2ETest : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(FileUploadNotificationE2ETest)}_";

        // All file upload notifications will be acknowledged with this type. We are deliberately
        // choosing to Abandon rather than Complete because each test process may receive file upload
        // notifications that another test was looking for. By abandoning each received notification,
        // the service makes it available for redelivery to other open receivers.
        private readonly AcknowledgementType _acknowledgementType = AcknowledgementType.Abandon;

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task FileUploadNotification_Operation(IotHubTransportProtocol protocol)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                Protocol = protocol
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            FileUploadNotificationCounter counter = new FileUploadNotificationCounter();
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                counter.FileUploadNotificationsReceived++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);
            await WaitForFileUploadNotification(counter, 1);
            await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task FileUploadNotification_Operation_OpenCloseOpen(IotHubTransportProtocol protocol)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                Protocol = protocol
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            FileUploadNotificationCounter counter = new FileUploadNotificationCounter();
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                counter.FileUploadNotificationsReceived++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            // Close and re-open the client
            await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
            await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);

            // Client should still be able to receive file upload notifications after being closed and re-opened.
            await UploadFile().ConfigureAwait(false);
            await WaitForFileUploadNotification(counter, 1);
            await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task FileUploadNotification_ReceiveMultipleNotificationsInOneConnection(IotHubTransportProtocol protocol)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                Protocol = protocol
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            FileUploadNotificationCounter counter = new FileUploadNotificationCounter();
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                counter.FileUploadNotificationsReceived++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);

            // The open file upload notification processor should be able to receive more than one
            // file upload notification without closing and re-opening as long as there is more
            // than one file upload notification to consume.
            await WaitForFileUploadNotification(counter, 2);
            await serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Wait until the expected number of file upload notifications have been received. If the expected
        /// number of notifications are not received in time, this method throws a AssertionFailedException.
        /// </summary>
        /// <param name="fileUploadNotificationReceivedCount">The current number of file upload notifications received.</param>
        /// <param name="expectedFileUploadNotificationReceivedCount">The expected number of file upload notifications to receive in this test.</param>
        private async Task WaitForFileUploadNotification(FileUploadNotificationCounter counter, int expectedFileUploadNotificationReceivedCount)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                // Note that this test may receive notifications from other file upload tests, so the received count may be higher
                // than the expected count.
                while (counter.FileUploadNotificationsReceived < expectedFileUploadNotificationReceivedCount)
                {
                    if (timer.ElapsedMilliseconds > 200000)
                    {
                        throw new AssertFailedException($"Timed out waiting for the expected number of file upload notifications. Received {counter.FileUploadNotificationsReceived}, expected {expectedFileUploadNotificationReceivedCount}");
                    }

                    await Task.Delay(800);
                }
            }
            finally
            {
                timer.Stop();
            }
        }

        private async Task UploadFile()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));
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
            FileUploadSasUriResponse sasUri = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest).ConfigureAwait(false);
            Uri uploadUri = sasUri.GetBlobUri();

            var blob = new CloudBlockBlob(uploadUri);
            await blob.UploadFromStreamAsync(fileStreamSource).ConfigureAwait(false);

            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
            {
                CorrelationId = sasUri.CorrelationId,
                IsSuccess = true,
                StatusCode = 200,
                StatusDescription = "Success"
            };

            await deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification).ConfigureAwait(false);
        }

        // This class exists to facilitate passing around an integer by reference. It is incremented
        // in a callback function and has its value checked in the WaitForFileUploadNotification function.
        private class FileUploadNotificationCounter
        {
            public FileUploadNotificationCounter()
            {
                FileUploadNotificationsReceived = 0;
            }

            public int FileUploadNotificationsReceived;
        }
    }
}
