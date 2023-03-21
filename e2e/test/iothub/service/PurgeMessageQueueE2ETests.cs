// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for PurgeMesageQueue.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class PurgeMesageQueueE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(PurgeMesageQueueE2ETests)}_";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task PurgeMessageQueueOperation()
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            string expectedDeviceId = testDevice.Device.Id;
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            // making sure the queue is empty
            PurgeMessageQueueResult result = await sc.Messages.PurgeMessageQueueAsync(expectedDeviceId, CancellationToken.None).ConfigureAwait(false);

            var testMessage = new OutgoingMessage(Encoding.UTF8.GetBytes("some payload"));

            await sc.Messages.OpenAsync().ConfigureAwait(false);
            const int numberOfSends = 3;
            for (int i = 0; i < numberOfSends; ++i)
            {
                await sc.Messages.SendAsync(expectedDeviceId, testMessage);
            }
            await sc.Messages.CloseAsync().ConfigureAwait(false);
            result = await sc.Messages.PurgeMessageQueueAsync(expectedDeviceId, CancellationToken.None).ConfigureAwait(false);
            result.DeviceId.Should().Be(expectedDeviceId);
            result.TotalMessagesPurged.Should().Be(numberOfSends);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task PurgeMessageQueueOperation_InvalidDeviceId_ThrowsIotHubServiceExeception()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            string faultyId = "some wrong Id";
            string expectedDeviceId = testDevice.Device.Id;
            IotHubServiceClient sc = TestDevice.ServiceClient;

            // act
            // making sure the queue is empty
            PurgeMessageQueueResult result = await sc.Messages.PurgeMessageQueueAsync(expectedDeviceId, CancellationToken.None).ConfigureAwait(false);
            var testMessage = new OutgoingMessage(Encoding.UTF8.GetBytes("some payload"));
            await sc.Messages.OpenAsync().ConfigureAwait(false);

            // assert
            Func<Task> act = async () => await sc.Messages.PurgeMessageQueueAsync(faultyId, CancellationToken.None).ConfigureAwait(false);
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
        }
    }
}
