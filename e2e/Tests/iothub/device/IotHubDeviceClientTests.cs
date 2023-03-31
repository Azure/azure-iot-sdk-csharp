// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class IotHubDeviceClientTests
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_AfterExplicitOpenAsync_DoesNotThrow()
        {
            // arrange
            using var createTestDeviceCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClientTests), ct: createTestDeviceCts.Token);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await testDevice.OpenWithRetryAsync(openCts.Token).ConfigureAwait(false);

            // act
            Func<Task> act = async () =>
            {
                using var sendTelemetryCts = new CancellationTokenSource(s_defaultOperationTimeout);
                await deviceClient.SendTelemetryAsync(new TelemetryMessage(), sendTelemetryCts.Token).ConfigureAwait(false);
            };

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
