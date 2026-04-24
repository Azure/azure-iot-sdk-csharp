// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DeviceClientE2eTests
    {
        [TestMethod]
        public async Task DeviceClient_CloseAsync_CanBeCalledTwice()
        {
            // arrange

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(DeviceClient_CloseAsync_CanBeCalledTwice));

            try
            {
                using Client.DeviceClient client = testDevice.CreateDeviceClient(Client.TransportType.Amqp_Tcp_Only);
                await client.OpenAsync().ConfigureAwait(false);
                await client.CloseAsync().ConfigureAwait(false);

                // act
                Func<Task> act = () => client.CloseAsync();

                // assert
                await act.Should().NotThrowAsync().ConfigureAwait(false);
            }
            finally
            {
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }
    }
}
