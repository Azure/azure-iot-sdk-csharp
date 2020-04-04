﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class FileUploadFaultInjectionTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(FileUploadFaultInjectionTests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        private ConsoleEventListener _listener;

        public FileUploadFaultInjectionTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task FileUploadSuccess_TcpLoss_Amqp()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);

            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    bigFile,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FileUploadSuccess_Throttled_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    smallFile,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task FileUploadSuccess_QuotaExceed_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    smallFile,
                    FaultInjection.FaultType_QuotaExceeded,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        private async Task UploadFileDisconnectTransport(
            Client.TransportType transport,
            string filename,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec = 0,
            int retryDurationInMilliSec = FaultInjection.RecoveryTimeMilliseconds)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;

                await FileNotificationTestListener.InitAsync().ConfigureAwait(false);

                using (var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    Task fileUploadTask = deviceClient.UploadToBlobAsync(filename, fileStreamSource);
                    Task errorInjectionTask = SendErrorInjectionMessageAsync(deviceClient, faultType, reason, delayInSec, durationInSec);
                    await Task.WhenAll(fileUploadTask, errorInjectionTask).ConfigureAwait(false);

                    await FileNotificationTestListener.VerifyFileNotification(filename, testDevice.Id).ConfigureAwait(false);
                }

                try
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    // catch and ignore exceptions resulted incase device client close failed while offline
                }
            }
        }

        private static async Task SendErrorInjectionMessageAsync(
            DeviceClient deviceClient,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec)
        {
            try
            {
                await deviceClient.SendEventAsync(FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec)).ConfigureAwait(false);
            }
            catch
            {
                // catch and ignore exceptions resulted from error injection and continue to check result of the file upload status
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
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
        }
    }
}
