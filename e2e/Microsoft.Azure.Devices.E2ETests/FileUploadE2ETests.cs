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

namespace Microsoft.Azure.Devices.E2ETests
{
    [Ignore] // TODO: Re-enable file upload tests for IoT Edge public preview
    [TestClass]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Uses custom scheme for cleanup")]
    public class FileUploadE2ETests
    {
        private const string DevicePrefix = "E2E_FileUpload_CSharp_";
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;
        private static ServiceClient serviceClient;
        private static FileNotificationReceiver<FileNotification> fileNotificationReceiver;

        private SemaphoreSlim sequentialTestSemaphore = new SemaphoreSlim(1, 1);

        private const string smallFile = "FileUpload_test_small.txt";
        private const string bigFile = "FileUpload_test_big.txt";

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment("E2E_FileUpload_CSharp_");
            hubConnectionString = environment.Item1;
            registryManager = environment.Item2;
            hostName = TestUtil.GetHostName(hubConnectionString);

            serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            fileNotificationReceiver = serviceClient.GetFileNotificationReceiver();

            File.WriteAllBytes(smallFile, new byte[10 * 1024]);
            File.WriteAllBytes(bigFile, new byte[5120 * 1024]);
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            serviceClient.CloseAsync().Wait();
            System.IO.File.Delete(smallFile);
            System.IO.File.Delete(bigFile);
            TestUtil.UnInitializeEnvironment(registryManager).GetAwaiter().GetResult();
        }

#if NETSTANDARD1_3
        [TestInitialize]
        public async Task Initialize()
        {
            await sequentialTestSemaphore.WaitAsync();
        }
#else
        [TestInitialize]
        public void Initialize()
        {
            sequentialTestSemaphore.Wait();
        }
#endif

        [TestCleanup]
        public void Cleanup()
        {
            sequentialTestSemaphore.Release(1);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_Http()
        {
            await uploadFile(Client.TransportType.Http1, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_Http()
        {
            await uploadFile(Client.TransportType.Http1, bigFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_X509_SmallFile_Http()
        {
           await uploadFile(Client.TransportType.Http1, smallFile, true);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        [TestCategory("Recovery")]
        public async Task FileUploadSuccess_TcpLoss_Amqp()
        {
            await uploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                bigFile,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec
                );
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        [TestCategory("Recovery")]
        public async Task FileUploadSuccess_Throttled_Amqp()
        {
            await uploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                smallFile,
                TestUtil.FaultType_Throttle,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec
                );
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        [TestCategory("Recovery")]
        public async Task FileUploadSuccess_QuotaExceed_Amqp()
        {
            await uploadFileDisconnectTransport(Client.TransportType.Amqp_Tcp_Only,
                smallFile,
                TestUtil.FaultType_QuotaExceeded,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec
                );
        }

        async Task uploadFile(Client.TransportType transport, string filename, bool x509auth = false)
        {
            DeviceClient deviceClient;
            Tuple<string, string> deviceInfo;
            if (x509auth)
            {
                deviceInfo = TestUtil.CreateDeviceWithX509(DevicePrefix, hostName, registryManager);

                X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

                var auth = new DeviceAuthenticationWithX509Certificate(deviceInfo.Item1, cert);
                deviceClient = DeviceClient.Create(deviceInfo.Item2, auth, transport);
            }
            else
            {
                deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
                deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            }

            using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                await deviceClient.UploadToBlobAsync(filename, fileStreamSource);
            }

            FileNotification fileNotification = await VerifyFileNotification(deviceInfo);

            Assert.IsNotNull(fileNotification, "FileNotification is not received.");
            Assert.AreEqual(deviceInfo.Item1 + "/" + filename, fileNotification.BlobName, "Uploaded file name mismatch in notifications");
            Assert.AreEqual(new FileInfo(filename).Length, fileNotification.BlobSizeInBytes, "Uploaded file size mismatch in notifications");
            Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");

            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private static async Task<FileNotification> VerifyFileNotification(Tuple<string, string> deviceInfo)
        {
            FileNotification fileNotification = null;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.Minutes < 2)
            {
                // Receive the file notification from queue
                fileNotification = await fileNotificationReceiver.ReceiveAsync(TimeSpan.FromSeconds(20));
                if (fileNotification != null)
                {
                    if (fileNotification.DeviceId == deviceInfo.Item1)
                    {
                        await fileNotificationReceiver.CompleteAsync(fileNotification);
                        break;
                    }

                    await fileNotificationReceiver.AbandonAsync(fileNotification);
                    fileNotification = null;
                }
            }
            sw.Stop();
            return fileNotification;
        }

        async Task uploadFileDisconnectTransport(Client.TransportType transport, string filename, string faultType, string reason, int delayInSec, 
            int durationInSec = 0, int retryDurationInMilliSec = 24000)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;

            Task fileuploadTask;
            Task<FileNotification> verifyTask;
            using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                verifyTask = VerifyFileNotification(deviceInfo);
                fileuploadTask = deviceClient.UploadToBlobAsync(filename, fileStreamSource);

                try
                {
                    await
                        deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason,
                            delayInSec, durationInSec));
                }
                catch (Exception)
                {
                    // catch and ignore exceptions resulted from error injection and continue to 
                    // check result of the file upload status
                }

                await Task.WhenAll(fileuploadTask, verifyTask);
            }

            FileNotification fileNotification = await verifyTask;

            Assert.IsNotNull(fileNotification, "FileNotification is not received.");
            Assert.AreEqual(deviceInfo.Item1 + "/" + filename, fileNotification.BlobName, "Uploaded file name mismatch in notifications");
            Assert.AreEqual(new FileInfo(filename).Length, fileNotification.BlobSizeInBytes, "Uploaded file size mismatch in notifications");
            Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");

            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }
    }
}
