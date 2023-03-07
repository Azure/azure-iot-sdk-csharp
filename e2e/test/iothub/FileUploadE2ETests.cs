// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(FileUploadE2ETests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        [Obsolete]
        public async Task FileUpload_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            // UploadFileAsync is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileAsync(Client.TransportType.Http1, smallFile).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_GetFileUploadSasUri_Http_NoFileTransportSettingSpecified()
        {
            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await GetSasUriAsync(Client.TransportType.Http1, smallFileBlobName).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_GetFileUploadSasUri_Http_x509_NoFileTransportSettingSpecified()
        {
            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await GetSasUriAsync(Client.TransportType.Http1, smallFileBlobName, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        [Obsolete]
        public async Task FileUpload_BigFile_Http()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);
            // UploadFileAsync is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileAsync(Client.TransportType.Http1, bigFile).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        [Obsolete]
        public async Task FileUpload_X509_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            // UploadFileAsync is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileAsync(Client.TransportType.Http1, smallFile, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_x509()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, x509auth: true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        [TestCategory("Proxy")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_Proxy()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings()
            {
                Proxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        private async Task UploadFileGranularAsync(Stream source, string filename, Http1TransportSettings fileUploadTransportSettings, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                Logger,
                _devicePrefix,
                x509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            DeviceClient deviceClient;
            var clientOptions = new ClientOptions()
            {
                FileUploadTransportSettings = fileUploadTransportSettings
            };

            if (x509auth)
            {
                X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

                var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);

                // The X509 certificate being used for device authentication needs to be set into FileUploadTransportSettings as well,
                // so that the HttpClient created for file upload operation has access to those certificates.
                clientOptions.FileUploadTransportSettings.ClientCertificate = cert;
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

            FileUploadSasUriResponse fileUploadSasUriResponse = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest).ConfigureAwait(false);

            var blob = new CloudBlockBlob(fileUploadSasUriResponse.GetBlobUri());
            Task uploadTask = blob.UploadFromStreamAsync(source);
            await uploadTask.ConfigureAwait(false);

            var notification = new FileUploadCompletionNotification
            {
                CorrelationId = fileUploadSasUriResponse.CorrelationId,
                IsSuccess = uploadTask.IsCompleted
            };

            await deviceClient.CompleteFileUploadAsync(notification).ConfigureAwait(false);
        }

        [Obsolete]
        private async Task UploadFileAsync(Client.TransportType transport, string filename, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                Logger,
                _devicePrefix,
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

            using (deviceClient)
            {
                using (var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    // UploadToBlobAsync is obsolete, added [Obsolete] attribute to suppress CS0618 message
                    await deviceClient.UploadToBlobAsync(filename, fileStreamSource).ConfigureAwait(false);
                }

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task GetSasUriAsync(Client.TransportType transport, string blobName, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                Logger,
                _devicePrefix,
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

            using (deviceClient)
            {
                FileUploadSasUriResponse sasUriResponse = await deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest { BlobName = blobName });
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
    }
}
