// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for testing receiving file upload notifications.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadNotificationE2eTest : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(FileUploadNotificationE2eTest)}_";

        // All file upload notifications will be acknowledged with this type. We are deliberately
        // choosing to Abandon rather than Complete because each test process may receive file upload
        // notifications that another test was looking for. By abandoning each received notification,
        // the service makes it available for redelivery to other open receivers.
        private readonly AcknowledgementType _acknowledgementType = AcknowledgementType.Abandon;

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(TransportType.Amqp)]
        [DataRow(TransportType.Amqp_WebSocket)]
        public async Task FileUploadNotification_Operation(TransportType transportType)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                UseWebSocketOnly = transportType == TransportType.Amqp_WebSocket,
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);

            int fileUploadNotificationReceivedCount = 0;
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                fileUploadNotificationReceivedCount++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);
            WaitForFileUploadNotification(ref fileUploadNotificationReceivedCount, 1);
            await serviceClient.FileUploadNotificationProcessor.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(TransportType.Amqp)]
        [DataRow(TransportType.Amqp_WebSocket)]
        public async Task FileUploadNotification_Operation_OpenCloseOpen(TransportType transportType)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                UseWebSocketOnly = transportType == TransportType.Amqp_WebSocket,
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);

            int fileUploadNotificationReceivedCount = 0;
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                fileUploadNotificationReceivedCount++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            // Close and re-open the client
            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);
            await serviceClient.FileUploadNotificationProcessor.CloseAsync().ConfigureAwait(false);
            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);

            // Client should still be able to receive file upload notifications after being closed and re-opened.
            await UploadFile().ConfigureAwait(false);
            WaitForFileUploadNotification(ref fileUploadNotificationReceivedCount, 1);
            await serviceClient.FileUploadNotificationProcessor.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(TransportType.Amqp)]
        [DataRow(TransportType.Amqp_WebSocket)]
        public async Task FileUploadNotification_ReceiveMultipleNotificationsInOneConnection(TransportType transportType)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions()
            {
                UseWebSocketOnly = transportType == TransportType.Amqp_WebSocket,
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);

            int fileUploadNotificationReceivedCount = 0;
            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                fileUploadNotificationReceivedCount++;
                return _acknowledgementType;
            };

            serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);
            await UploadFile().ConfigureAwait(false);

            // The open file upload notification processor should be able to receive more than one
            // file upload notification without closing and re-opening as long as there is more
            // than one file upload notification to consume.
            WaitForFileUploadNotification(ref fileUploadNotificationReceivedCount, 2);
            await serviceClient.FileUploadNotificationProcessor.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Wait until the expected number of file upload notifications have been received. If the expected
        /// number of notifications are not received in time, this method throws a AssertionFailedException.
        /// </summary>
        /// <param name="fileUploadNotificationReceivedCount">The current number of file upload notifications received.</param>
        /// <param name="expectedFileUploadNotificationReceivedCount">The expected number of file upload notifications to receive in this test.</param>
        private void WaitForFileUploadNotification(ref int fileUploadNotificationReceivedCount, int expectedFileUploadNotificationReceivedCount)
        {
            var timer = Stopwatch.StartNew();
            while (fileUploadNotificationReceivedCount < expectedFileUploadNotificationReceivedCount && timer.ElapsedMilliseconds < 60000)
            {
                Thread.Sleep(200);
            }

            timer.Stop();

            // Note that this test may receive notifications from other file upload tests, so the received count may be higher
            // than the expected count.
            fileUploadNotificationReceivedCount.Should().BeGreaterOrEqualTo(expectedFileUploadNotificationReceivedCount,
                "Timed out waiting to receive file upload notification.");
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
    }
}
