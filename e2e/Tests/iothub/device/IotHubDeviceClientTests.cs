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
    public class IotHubDeviceClientTests : E2EMsTestBase
    {
        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_AfterExplicitOpenAsync_DoesNotThrow()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClientTests), ct);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.SendTelemetryAsync(new TelemetryMessage(), ct).ConfigureAwait(false);
            };

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
