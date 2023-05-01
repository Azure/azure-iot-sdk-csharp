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
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClientTests), ct: ct);
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

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task IotHubDeviceClient_CloseAsync_CanBeCalledTwice(bool useMqtt)
        {
            // arrange

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClient_CloseAsync_CanBeCalledTwice));

            IotHubClientTransportSettings transport = useMqtt
                ? new IotHubClientMqttSettings()
                : new IotHubClientAmqpSettings();

            IotHubDeviceClient client = testDevice.CreateDeviceClient(new IotHubClientOptions(transport));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await testDevice.OpenWithRetryAsync(cts.Token).ConfigureAwait(false);
            await client.CloseAsync().ConfigureAwait(false);

            // act
            Func<Task> act = () => client.CloseAsync();

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task IotHubDeviceClient_DisposeAsync_CanBeCalledTwice(bool useMqtt)
        {
            // arrange

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(IotHubDeviceClient_DisposeAsync_CanBeCalledTwice));

            IotHubClientTransportSettings transport = useMqtt
                ? new IotHubClientMqttSettings()
                : new IotHubClientAmqpSettings();

            IotHubDeviceClient client = testDevice.CreateDeviceClient(new IotHubClientOptions(transport));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await testDevice.OpenWithRetryAsync(cts.Token).ConfigureAwait(false);
            await client.DisposeAsync().ConfigureAwait(false);

            // act
            Func<Task> act = () => client.DisposeAsync().AsTask();

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
