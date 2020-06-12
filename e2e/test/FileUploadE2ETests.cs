﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(FileUploadE2ETests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

#pragma warning disable CA1823
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();
#pragma warning restore CA1823

        public FileUploadE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, smallFile).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_BigFile_Http()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, bigFile).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_X509_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, smallFile, true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Http1TransportSettings fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileManual(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_x509()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Http1TransportSettings fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileManual(fileStreamSource, filename, fileUploadTransportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_Proxy()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Http1TransportSettings fileUploadTransportSettings = new Http1TransportSettings()
            {
                Proxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            await UploadFileManual(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        private async Task UploadFileManual(Stream source, string filename, Http1TransportSettings fileUploadTransportSettings, bool x509auth = false)
        {
            await FileNotificationTestListener.InitAsync().ConfigureAwait(false);

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                DevicePrefix,
                x509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            DeviceClient deviceClient;
            ClientOptions clientOptions = new ClientOptions()
            {
                FileUploadTransportSettings = fileUploadTransportSettings
            };

            if (x509auth)
            {
                X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

                var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);
                deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, auth, Client.TransportType.Http1, clientOptions);
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Http1, clientOptions);
            }

            var fileUploadSasUriRequest = new FileUploadSasUriRequest()
            {
                BlobName = filename
            };
            var fileUploadSasUriResponse = await deviceClient.GetFileUploadSasUri(fileUploadSasUriRequest).ConfigureAwait(false);

            string blobUriString = String.Format(
                CultureInfo.InvariantCulture,
                "https://{0}/{1}/{2}{3}",
                fileUploadSasUriResponse.HostName,
                fileUploadSasUriResponse.ContainerName,
                Uri.EscapeDataString(fileUploadSasUriResponse.BlobName), // Pass URL encoded device name and blob name to support special characters
                fileUploadSasUriResponse.SasToken);

            CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUriString));
            var uploadTask = blob.UploadFromStreamAsync(source);
            await uploadTask.ConfigureAwait(false);

            var notification = new FileUploadCompletionNotification();
            notification.CorrelationId = fileUploadSasUriResponse.CorrelationId;
            notification.IsSuccess = uploadTask.IsCompleted;
            notification.StatusCode = uploadTask.IsCompleted ? 0 : -1;
            notification.StatusDescription = uploadTask.IsCompleted ? null : "Failed to upload to storage.";

            await deviceClient.CompleteFileUploadAsync(notification).ConfigureAwait(false);

            await FileNotificationTestListener.VerifyFileNotification(filename, testDevice.Id).ConfigureAwait(false);
        }

        private async Task UploadFile(Client.TransportType transport, string filename, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                DevicePrefix,
                x509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            DeviceClient deviceClient;
            if (x509auth)
            {
                X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

                var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);
                deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, auth, transport);
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            }

            await FileNotificationTestListener.InitAsync().ConfigureAwait(false);

            using (deviceClient)
            {
                using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    await deviceClient.UploadToBlobAsync(filename, fileStreamSource).ConfigureAwait(false);
                }

                await FileNotificationTestListener.VerifyFileNotification(filename, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task<string> GetTestFileNameAsync(int fileSize)
        {
            var rnd = new Random();
            byte[] buffer = new byte[fileSize];
            rnd.NextBytes(buffer);

            string filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

#if NET451 || NET472
            File.WriteAllBytes(filePath, buffer);
            await Task.Delay(0).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(filePath, buffer).ConfigureAwait(false);
#endif

            return filePath;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
