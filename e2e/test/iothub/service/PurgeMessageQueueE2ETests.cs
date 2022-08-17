// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for PurgeMesageQueue.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class PurgeMesageQueueE2eTests : E2EMsTestBase
    {
        [TestMethod]
        public async Task PurgeMessageQueueOperation()
        {
            Message testMessage = ComposeD2CTestMessage();
            using var sc = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            var deviceId = TestConfiguration.IoTHub.X509ChainDeviceName;
            var expectedResult = new PurgeMessageQueueResult
            {
                DeviceId = deviceId,
                TotalMessagesPurged = 3
            };
            for (int i = 0; i < 3; ++i)
            {
                await sc.Messaging.SendAsync(deviceId, testMessage);
            }
            PurgeMessageQueueResult result = await sc.Messaging.PurgeMessageQueueAsync(deviceId, CancellationToken.None).ConfigureAwait(false);
            result.DeviceId.Should().Be(deviceId);
            result.TotalMessagesPurged.Should().Be(expectedResult.TotalMessagesPurged);
        }

        private Message ComposeD2CTestMessage()
        {
            return new Message(Encoding.UTF8.GetBytes("some payload"));
        }
    }
}
