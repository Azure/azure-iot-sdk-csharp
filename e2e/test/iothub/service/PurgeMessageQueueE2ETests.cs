// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
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
        private readonly string _devicePrefix = $"{nameof(PurgeMesageQueueE2eTests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task PurgeMessageQueueOperation()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var deviceId = testDevice.Device.Id;
            using var sc = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            PurgeMessageQueueResult result = await sc.Messaging.PurgeMessageQueueAsync(deviceId, CancellationToken.None).ConfigureAwait(false); // making sure the queue is empty

            Message testMessage = ComposeD2CTestMessage();
            var expectedResult = new PurgeMessageQueueResult
            {
                DeviceId = deviceId,
                TotalMessagesPurged = 3
            };

            await sc.Messaging.OpenAsync().ConfigureAwait(false);
            for (int i = 0; i < 3; ++i)
            {
                await sc.Messaging.SendAsync(deviceId, testMessage);
            }
            await sc.Messaging.CloseAsync().ConfigureAwait(false);
            result = await sc.Messaging.PurgeMessageQueueAsync(deviceId, CancellationToken.None).ConfigureAwait(false);
            result.DeviceId.Should().Be(deviceId);
            result.TotalMessagesPurged.Should().Be(expectedResult.TotalMessagesPurged);
        }

        private Message ComposeD2CTestMessage()
        {
            return new Message(Encoding.UTF8.GetBytes("some payload"));
        }
    }
}
