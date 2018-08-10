// Copyright (c) Microsoft. All rights reserved.
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
        private const string DevicePrefix = "E2E_FileUpload_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        private readonly ConsoleEventListener _listener;

        public FileUploadE2ETests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
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

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task FileUploadSuccess_TcpLoss_Amqp()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);

            await UploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                bigFile,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec
                ).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task FileUploadSuccess_Throttled_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            await UploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                smallFile,
                FaultInjection.FaultType_Throttle,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec
                ).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task FileUploadSuccess_QuotaExceed_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            await UploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                smallFile,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec
                ).ConfigureAwait(false);
        }

        private async Task UploadFile(Client.TransportType transport, string filename, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                DevicePrefix, 
                x509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            FileNotificationReceiver<FileNotification> notificationReceiver = serviceClient.GetFileNotificationReceiver();

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

            using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                await deviceClient.UploadToBlobAsync(filename, fileStreamSource).ConfigureAwait(false);
            }

            FileNotification fileNotification = await VerifyFileNotification(notificationReceiver, testDevice.Id).ConfigureAwait(false);

            Assert.IsNotNull(fileNotification, "FileNotification is not received.");
            Assert.AreEqual(testDevice.Id + "/" + filename, fileNotification.BlobName, "Uploaded file name mismatch in notifications");
            Assert.AreEqual(new FileInfo(filename).Length, fileNotification.BlobSizeInBytes, "Uploaded file size mismatch in notifications");
            Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task<FileNotification> VerifyFileNotification(FileNotificationReceiver<FileNotification> fileNotificationReceiver, string deviceId)
        {
            FileNotification fileNotification = null;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.Minutes < 2)
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

        private async Task UploadFileDisconnectTransport(
            Client.TransportType transport, 
            string filename, 
            string faultType, 
            string reason, 
            int delayInSec, 
            int durationInSec = 0, 
            int retryDurationInMilliSec = 24000)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            FileNotificationReceiver<FileNotification> notificationReceiver = serviceClient.GetFileNotificationReceiver();

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;

            Task fileuploadTask;
            Task<FileNotification> verifyTask;
            using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                verifyTask = VerifyFileNotification(notificationReceiver, testDevice.Id);
                fileuploadTask = deviceClient.UploadToBlobAsync(filename, fileStreamSource);

                try
                {
                    await
                        deviceClient.SendEventAsync(FaultInjection.ComposeErrorInjectionProperties(faultType, reason,
                            delayInSec, durationInSec)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // catch and ignore exceptions resulted from error injection and continue to 
                    // check result of the file upload status
                }

                await Task.WhenAll(fileuploadTask, verifyTask).ConfigureAwait(false);
            }

            FileNotification fileNotification = await verifyTask.ConfigureAwait(false);

            Assert.IsNotNull(fileNotification, "FileNotification is not received.");
            Assert.AreEqual(testDevice.Id + "/" + filename, fileNotification.BlobName, "Uploaded file name mismatch in notifications");
            Assert.AreEqual(new FileInfo(filename).Length, fileNotification.BlobSizeInBytes, "Uploaded file size mismatch in notifications");
            Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
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
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
