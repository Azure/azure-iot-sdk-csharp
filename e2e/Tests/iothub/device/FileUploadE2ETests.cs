﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class FileUploadE2ETests : E2EMsTestBase
    {
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;
        private readonly string _devicePrefix = $"{nameof(FileUploadE2ETests)}_";
        private static readonly X509Certificate2 s_selfSignedCertificate = TestConfiguration.IotHub.GetCertificateWithPrivateKey();

        [TestMethod]
        public async Task FileUpload_GetFileUploadSasUri_Mqtt_x509_NoFileTransportSettingSpecified()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            await GetSasUriAsync(new IotHubClientMqttSettings(), new IotHubClientHttpSettings(), smallFileBlobName, true, cts.Token).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_GetFileUploadSasUri_Amqp_x509_NoFileTransportSettingSpecified()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            await GetSasUriAsync(new IotHubClientAmqpSettings(), new IotHubClientHttpSettings(), smallFileBlobName, true, cts.Token).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_SmallFile_GranularSteps_ValidCorrelationId()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string filename = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new IotHubClientHttpSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, isCorrelationIdValid: true, ct: cts.Token).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_SmallFile_GranularSteps_InvalidCorrelationId()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string filename = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new IotHubClientHttpSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, isCorrelationIdValid: false, ct: cts.Token).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_SmallFile_GranularSteps_x509()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string filename = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new IotHubClientHttpSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, isCorrelationIdValid: true, useX509auth: true, ct: cts.Token).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_SmallFile_GranularSteps_Proxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);

            string filename = await GetTestFileNameAsync(FileSizeSmall, cts.Token).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new IotHubClientHttpSettings()
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, isCorrelationIdValid: true, ct: cts.Token).ConfigureAwait(false);
        }

        private async Task UploadFileGranularAsync(
            Stream source,
            string filename,
            IotHubClientHttpSettings fileUploadTransportSettings,
            bool isCorrelationIdValid,
            bool useX509auth = false,
            CancellationToken ct = default)
        {
            await using TestDevice testDevice = await TestDevice
                .GetTestDeviceAsync(
                    _devicePrefix,
                    useX509auth ? TestDeviceType.X509 : TestDeviceType.Sasl,
                    ct)
                .ConfigureAwait(false);

            IotHubDeviceClient deviceClient;
            var clientOptions = new IotHubClientOptions
            {
                FileUploadTransportSettings = fileUploadTransportSettings
            };

            X509Certificate2 cert = null;
            ClientAuthenticationWithX509Certificate x509Auth = null;
            if (useX509auth)
            {
                cert = s_selfSignedCertificate;
                x509Auth = new ClientAuthenticationWithX509Certificate(cert, testDevice.Id);

                deviceClient = new IotHubDeviceClient(testDevice.IotHubHostName, x509Auth, new IotHubClientOptions { FileUploadTransportSettings = fileUploadTransportSettings });
            }
            else
            {
                deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, clientOptions);
            }
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            var fileUploadSasUriRequest = new FileUploadSasUriRequest(filename);

            await using (deviceClient)
            {
                FileUploadSasUriResponse fileUploadSasUriResponse = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest, ct).ConfigureAwait(false);

                var blob = new CloudBlockBlob(fileUploadSasUriResponse.GetBlobUri());
                Task uploadTask = blob.UploadFromStreamAsync(source, null, null, null, null, ct);
                await uploadTask.ConfigureAwait(false);

                if (isCorrelationIdValid)
                {
                    var notification = new FileUploadCompletionNotification(fileUploadSasUriResponse.CorrelationId, uploadTask.IsCompleted);

                    await deviceClient.CompleteFileUploadAsync(notification, ct).ConfigureAwait(false);
                }
                else
                {
                    var notification = new FileUploadCompletionNotification("invalid-correlation-id", uploadTask.IsCompleted);

                    // act and assert

                    try
                    {
                        await deviceClient.CompleteFileUploadAsync(notification, ct).ConfigureAwait(false);
                    }
                    catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.ServerError)
                    {
                        // Gateway V1 flow
                    }
                    catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.IotHubFormatError)
                    {
                        // Gateway V2 flow
                        ex.Message.Should().Contain("Cannot decode correlation_id");
                        ex.TrackingId.Should().NotBe(string.Empty);
                        ex.IsTransient.Should().BeFalse();
                    }
                }
            }
        }

        private async Task GetSasUriAsync(
            IotHubClientTransportSettings clientMainTransport,
            IotHubClientHttpSettings fileUploadSettings,
            string blobName,
            bool useX509auth = false,
            CancellationToken ct = default)
        {
            await using TestDevice testDevice = await TestDevice
                .GetTestDeviceAsync(
                    _devicePrefix,
                    useX509auth
                        ? TestDeviceType.X509
                        : TestDeviceType.Sasl,
                    ct)
                .ConfigureAwait(false);

            var options = new IotHubClientOptions(clientMainTransport)
            {
                FileUploadTransportSettings = fileUploadSettings,
            };
            IotHubDeviceClient deviceClient;
            X509Certificate2 cert = null;
            ClientAuthenticationWithX509Certificate x509Auth = null;
            if (useX509auth)
            {
                cert = s_selfSignedCertificate;
                x509Auth = new ClientAuthenticationWithX509Certificate(cert, testDevice.Id);

                deviceClient = new IotHubDeviceClient(testDevice.IotHubHostName, x509Auth, options);
            }
            else
            {
                deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            }

            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            await using (deviceClient)
            {
                FileUploadSasUriResponse sasUriResponse = await deviceClient
                    .GetFileUploadSasUriAsync(new FileUploadSasUriRequest(blobName), ct)
                    .ConfigureAwait(false);
                sasUriResponse.Should().NotBeNull();
            }
        }

        private static async Task<string> GetTestFileNameAsync(int fileSize, CancellationToken ct)
        {
            var rnd = new Random();
            byte[] buffer = new byte[fileSize];
            rnd.NextBytes(buffer);

            string filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

#if NET472
            File.WriteAllBytes(filePath, buffer);
            await Task.Delay(0, ct).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(filePath, buffer, ct).ConfigureAwait(false);
#endif

            return filePath;
        }

        [ClassCleanup]
        public static void CleanupCertificates()
        {
            if (s_selfSignedCertificate is IDisposable disposableCertificate)
            {
                disposableCertificate?.Dispose();
            }
        }
    }
}
