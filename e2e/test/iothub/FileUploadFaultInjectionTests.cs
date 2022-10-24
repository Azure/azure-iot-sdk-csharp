// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class FileUploadFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(FileUploadFaultInjectionTests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [Obsolete]
        public async Task FileUploadSuccess_TcpLoss_Amqp()
        {
            string bigFile = await GetTestFileNameAsync(FileSizeBig).ConfigureAwait(false);

            // UploadFileDisconnectTransport is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    bigFile,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [Obsolete]
        public async Task FileUploadSuccess_Throttled_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            // UploadFileDisconnectTransport is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    smallFile,
                    FaultInjectionConstants.FaultType_Throttle,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize]
        [Obsolete]
        public async Task FileUploadSuccess_QuotaExceed_Amqp()
        {
            string smallFile = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            // UploadFileDisconnectTransport is marked obsolete due to a call to UploadToBlobAsync being obsolete
            // Added [Obsolete] attribute to this method to suppress CS0618 message
            await UploadFileDisconnectTransport(
                    Client.TransportType.Amqp_Tcp_Only,
                    smallFile,
                    FaultInjectionConstants.FaultType_QuotaExceeded,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration)
                .ConfigureAwait(false);
        }

        [Obsolete]
        private async Task UploadFileDisconnectTransport(
            Client.TransportType transport,
            string filename,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            TimeSpan durationInSec = default,
            TimeSpan retryDurationInMilliSec = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            TimeSpan operationTimeout = retryDurationInMilliSec == TimeSpan.Zero ? FaultInjection.RecoveryTime : retryDurationInMilliSec;
            deviceClient.OperationTimeoutInMilliseconds = (uint)operationTimeout.TotalMilliseconds;

            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            // UploadToBlobAsync is obsolete, added [Obsolete] attribute to suppress CS0618 message
            Task fileUploadTask = deviceClient.UploadToBlobAsync(filename, fileStreamSource);
            Task errorInjectionTask = SendErrorInjectionMessageAsync(deviceClient, faultType, reason, delayInSec, durationInSec);
            await Task.WhenAll(fileUploadTask, errorInjectionTask).ConfigureAwait(false);

            try
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
            catch
            {
                // catch and ignore exceptions resulted incase device client close failed while offline
            }
        }

        private static async Task SendErrorInjectionMessageAsync(
            DeviceClient deviceClient,
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration)
        {
            try
            {
                using Client.Message faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, faultDelay, faultDuration);
                await deviceClient.SendEventAsync(faultInjectionMessage).ConfigureAwait(false);
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
    }
}
