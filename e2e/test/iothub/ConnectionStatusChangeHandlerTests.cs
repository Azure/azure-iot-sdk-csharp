﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ConnectionStatusChangeHandlerTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(ConnectionStatusChangeHandlerTests)}_Device";
        private readonly string ModulePrefix = $"{nameof(ConnectionStatusChangeHandlerTests)}";

        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                    Client.TransportType.Amqp_Tcp_Only,
                    async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [TestCategory("LongRunning")]
        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                    Client.TransportType.Amqp_WebSocket_Only,
                    async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)] // This test always takes more than 5 minutes for service to return. Needs investigation.
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                    Client.TransportType.Amqp_Tcp_Only,
                    async (r, d) =>
                    {
                        Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                        device.Status = DeviceStatus.Disabled;
                        await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                    })
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only,
                async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only,
                async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                    Client.TransportType.Amqp_WebSocket_Only,
                    async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private async Task DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
            Client.TransportType protocol,
            Func<RegistryManager, string, Task> registryManagerOperation)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IotHub.ConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceId;

            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            int deviceDisabledReceivedCount = 0;

            var sw = Stopwatch.StartNew();
            using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, protocol);
            void statusChangeHandler(ConnectionStatus s, ConnectionStatusChangeReason r)
            {
                VerboseTestLogger.WriteLine($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)} connection status change {s}, {r} at {sw.Elapsed}.");
                if (r == ConnectionStatusChangeReason.Device_Disabled)
                {
                    status = s;
                    statusChangeReason = r;
                    deviceDisabledReceivedCount++;
                }
            }

            deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);
            VerboseTestLogger.WriteLine($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Created {nameof(DeviceClient)} with device Id={testDevice.Id}");

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Receiving the twin should succeed right now.
            VerboseTestLogger.WriteLine($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: DeviceClient GetTwinAsync.");
            Shared.Twin twin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            twin.Should().NotBeNull();

            // Delete/disable the device in IoT hub. This should trigger the ConnectionStatusChangesHandler.
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            sw.Restart();
            await registryManagerOperation(registryManager, deviceId).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection status change handler to get triggered.
            while (deviceDisabledReceivedCount <= 0)
            {
                VerboseTestLogger.WriteLine($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Still waiting for connection update {sw.Elapsed} after device status was changed.");
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }

            deviceDisabledReceivedCount.Should().Be(1);
            status.Should().Be(ConnectionStatus.Disconnected);
            statusChangeReason.Should().Be(ConnectionStatusChangeReason.Device_Disabled);
        }

        private async Task ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
            Client.TransportType protocol,
            Func<RegistryManager, string, Task> registryManagerOperation)
        {
            var amqpTransportSettings = new AmqpTransportSettings(protocol);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix).ConfigureAwait(false);
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            int deviceDisabledReceivedCount = 0;
            void statusChangeHandler(ConnectionStatus s, ConnectionStatusChangeReason r)
            {
                if (r == ConnectionStatusChangeReason.Device_Disabled)
                {
                    status = s;
                    statusChangeReason = r;
                    deviceDisabledReceivedCount++;
                }
            }

            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings);
            moduleClient.SetConnectionStatusChangesHandler(statusChangeHandler);
            VerboseTestLogger.WriteLine($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Created {nameof(ModuleClient)} with moduleId={testModule.Id}");

            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Receiving the module twin should succeed right now.
            VerboseTestLogger.WriteLine($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: ModuleClient GetTwinAsync.");
            Shared.Twin twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);
            Assert.IsNotNull(twin);

            // Delete/disable the device in IoT hub.
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            await registryManagerOperation(registryManager, testModule.DeviceId).ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection status change handler to get triggered.
            int sleepCount = 50;
            for (int i = 0; i < sleepCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (deviceDisabledReceivedCount == 1)
                {
                    break;
                }
            }

            deviceDisabledReceivedCount.Should().Be(1);
            status.Should().Be(ConnectionStatus.Disconnected);
            statusChangeReason.Should().Be(ConnectionStatusChangeReason.Device_Disabled);
        }
    }
}
