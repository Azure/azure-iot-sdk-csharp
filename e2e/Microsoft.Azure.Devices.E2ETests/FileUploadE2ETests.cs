using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class FileUploadE2ETests
    {
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

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

            File.WriteAllBytes(smallFile, new byte[10 * 1024]);
            File.WriteAllBytes(bigFile, new byte[5120 * 1024]);
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            System.IO.File.Delete(smallFile);
            System.IO.File.Delete(bigFile);
            TestUtil.UnInitializeEnvironment(registryManager);
        }

        [TestInitialize]
        public async void Initialize()
        {
            await sequentialTestSemaphore.WaitAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            sequentialTestSemaphore.Release(1);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_Amqp()
        {
            await uploadFile(Client.TransportType.Amqp_Tcp_Only, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_AmqpWs()
        {
            await uploadFile(Client.TransportType.Amqp_WebSocket_Only, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_Mqtt()
        {
            await uploadFile(Client.TransportType.Mqtt_Tcp_Only, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_MqttWs()
        {
            await uploadFile(Client.TransportType.Mqtt_WebSocket_Only, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_SmallFile_Http()
        {
            await uploadFile(Client.TransportType.Http1, smallFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_Amqp()
        {
            await uploadFile(Client.TransportType.Amqp_Tcp_Only, bigFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_AmqpWs()
        {
            await uploadFile(Client.TransportType.Amqp_WebSocket_Only, bigFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_Mqtt()
        {
            await uploadFile(Client.TransportType.Mqtt_Tcp_Only, bigFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_MqttWs()
        {
            await uploadFile(Client.TransportType.Mqtt_WebSocket_Only, bigFile);
        }

        [TestMethod]
        [TestCategory("FileUpload-E2E")]
        public async Task FileUpload_BigFile_Http()
        {
            await uploadFile(Client.TransportType.Http1, bigFile);
        }

        async Task uploadFile(Client.TransportType transport, string filename)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_FileUpload_CSharp_", hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            using (FileStream fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                await deviceClient.UploadToBlobAsync(filename, fileStreamSource);
            }

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            FileNotificationReceiver<FileNotification> fileNotificationReceiver = serviceClient.GetFileNotificationReceiver();

            FileNotification fileNotification;
            while (true)
            {
                // Receive the file notification from queue
                fileNotification = await fileNotificationReceiver.ReceiveAsync(TimeSpan.FromSeconds(20));
                Assert.IsNotNull(fileNotification);
                await fileNotificationReceiver.CompleteAsync(fileNotification);
                if (deviceInfo.Item1 == fileNotification.DeviceId)
                    break;
            }

            Assert.AreEqual(deviceInfo.Item1 + "/" + filename, fileNotification.BlobName, "Uploaded file name mismatch in notifications");
            Assert.AreEqual(new FileInfo(filename).Length, fileNotification.BlobSizeInBytes, "Uploaded file size mismatch in notifications");
            Assert.IsFalse(string.IsNullOrEmpty(fileNotification.BlobUri), "File notification blob uri is null or empty");

            await deviceClient.CloseAsync();
            await serviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}
