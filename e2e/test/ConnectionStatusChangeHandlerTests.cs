﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ConnectionStatusChangeHandlerTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(ConnectionStatusChangeHandlerTests)}_Device";
        private readonly string ModulePrefix = $"E2E_{nameof(ConnectionStatusChangeHandlerTests)}";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public ConnectionStatusChangeHandlerTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestCategory("LongRunning")]
        [TestMethod]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // IoT Hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [TestMethod]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        // IoT Hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [TestMethod]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) =>
            {
                Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                device.Status = DeviceStatus.Disabled;
                await r.UpdateDeviceAsync(device).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
            Client.TransportType protocol, Func<RegistryManager, string, Task> registryManagerOperation)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new Configuration.IoTHub.DeviceConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceID;

            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            int deviceDisabledReceivedCount = 0;

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, protocol))
            {
                ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
                {
                    if (r == ConnectionStatusChangeReason.Device_Disabled)
                    {
                        status = s;
                        statusChangeReason = r;
                        deviceDisabledReceivedCount++;
                    }
                };

                deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);
                _log.WriteLine($"Created {nameof(DeviceClient)} ID={TestLogging.IdOf(deviceClient)}");

                Console.WriteLine("DeviceClient OpenAsync.");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Receiving the module twin should succeed right now.
                Console.WriteLine("ModuleClient GetTwinAsync.");
                var twin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(twin);

                // Delete/disable the device in IoT Hub. This should trigger the ConnectionStatusChangesHandler.
                using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, deviceId).ConfigureAwait(false);
                }

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

                Assert.AreEqual(1, deviceDisabledReceivedCount);
                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }

        private async Task ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
            Client.TransportType protocol, Func<RegistryManager, string, Task> registryManagerOperation)
        {
            AmqpTransportSettings amqpTransportSettings = new AmqpTransportSettings(protocol);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix).ConfigureAwait(false);
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            int deviceDisabledReceivedCount = 0;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                if (r == ConnectionStatusChangeReason.Device_Disabled)
                {
                    status = s;
                    statusChangeReason = r;
                    deviceDisabledReceivedCount++;
                }
            };

            using (ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings))
            {
                moduleClient.SetConnectionStatusChangesHandler(statusChangeHandler);
                _log.WriteLine($"Created {nameof(DeviceClient)} ID={TestLogging.IdOf(moduleClient)}");

                Console.WriteLine("ModuleClient OpenAsync.");
                await moduleClient.OpenAsync().ConfigureAwait(false);

                // Receiving the module twin should succeed right now.
                Console.WriteLine("ModuleClient GetTwinAsync.");
                var twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(twin);

                // Delete/disable the device in IoT Hub.
                using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, testModule.DeviceId).ConfigureAwait(false);
                }

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

                Assert.AreEqual(1, deviceDisabledReceivedCount);
                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
