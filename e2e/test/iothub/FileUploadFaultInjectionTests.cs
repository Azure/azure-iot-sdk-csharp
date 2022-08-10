// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class FileUploadFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(FileUploadFaultInjectionTests)}_";
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;

        private static async Task SendErrorInjectionMessageAsync(
            IotHubDeviceClient deviceClient,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            TimeSpan durationInSec)
        {
            try
            {
                Client.Message faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec);
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

#if NET472
            File.WriteAllBytes(filePath, buffer);
            await Task.Delay(0).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(filePath, buffer).ConfigureAwait(false);
#endif

            return filePath;
        }
    }
}
