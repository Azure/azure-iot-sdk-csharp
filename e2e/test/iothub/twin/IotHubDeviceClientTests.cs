// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.twin
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class IotHubDeviceClientTests
    {

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_AfterExplicitOpenAsync_DoesNotThrow()
        {
            // arrange
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClientTests)).ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient.SendTelemetryAsync(new TelemetryMessage()).ConfigureAwait(false);
            };

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
