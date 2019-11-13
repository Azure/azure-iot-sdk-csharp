﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class FileUploadE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(FileUploadE2ETests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();
        
        public FileUploadE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task FileUpload_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, smallFile).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_BigFile_Http()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, bigFile).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUpload_X509_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await UploadFile(Client.TransportType.Http1, smallFile, true).ConfigureAwait(false);
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

            using(deviceClient)
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                FileNotificationReceiver<FileNotification> notificationReceiver = serviceClient.GetFileNotificationReceiver();
                using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    await deviceClient.UploadToBlobAsync(filename, fileStreamSource).ConfigureAwait(false);
                }

                FileNotification fileNotification = await VerifyFileNotification(notificationReceiver, testDevice.Id).ConfigureAwait(false);

                // The following checks allow running these tests multiple times in parallel. 
                // Notifications for one of the test-run instances may be received by the other test-run.
               
                Assert.IsNotNull(fileNotification, "FileNotification is not received.");
                _log.WriteLine($"TestDevice: '{testDevice.Id}', blobName: '{fileNotification.BlobName}', size: {fileNotification.BlobSizeInBytes}");
                Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");
                                
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task<FileNotification> VerifyFileNotification(FileNotificationReceiver<FileNotification> fileNotificationReceiver, string deviceId)
        {
            FileNotification fileNotification = null;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMinutes < 2)
            {
                // Receive the file notification from queue
                fileNotification = await fileNotificationReceiver.ReceiveAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
                if (fileNotification != null)
                {
                    if (fileNotification.DeviceId == deviceId)
                    {
                        await fileNotificationReceiver.CompleteAsync(fileNotification).ConfigureAwait(false);
                        break;
                    }

                    await fileNotificationReceiver.AbandonAsync(fileNotification).ConfigureAwait(false);
                    fileNotification = null;
                }
            }
            sw.Stop();
            return fileNotification;
        }

        private async Task<string> GetTestFileNameAsync(int fileSize)
        {
            var rnd = new Random();
            byte[] buffer = new byte[fileSize];
            rnd.NextBytes(buffer);

            string filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

#if NET451 || NET47
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
