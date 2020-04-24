// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            await UploadFile(GetDefaultTransportSettings(), smallFile).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_BigFile_Http()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);
            await UploadFile(GetDefaultTransportSettings(), bigFile).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_X509_SmallFile_Http()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await UploadFile(GetDefaultTransportSettings(), smallFile, true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_WithProxy()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            var httpTransportSettings = new Http1TransportSettings()
            {
                Proxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            ITransportSettings[] transportSettingsWithProxy = { httpTransportSettings };

            await UploadFile(transportSettingsWithProxy, smallFile).ConfigureAwait(false);
        }

        private ITransportSettings[] GetDefaultTransportSettings()
        {
            ITransportSettings[] transportSettings = { new Http1TransportSettings() };
            return transportSettings;
        }

        private async Task UploadFile(Client.ITransportSettings[] transportSettings, string filename, bool x509auth = false)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                DevicePrefix,
                x509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            DeviceClient deviceClient;
            if (x509auth)
            {
                X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

                var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);
                deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, auth, transportSettings);
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportSettings);
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
