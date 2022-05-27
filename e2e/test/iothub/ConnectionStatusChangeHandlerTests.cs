// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
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
        private readonly string DevicePrefix = $"E2E_{nameof(ConnectionStatusChangeHandlerTests)}_Device";
        private readonly string ModulePrefix = $"E2E_{nameof(ConnectionStatusChangeHandlerTests)}";

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestCategory("LongRunning")]
        [LoggedTestMethod]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                Client.TransportType.Amqp_Tcp_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
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
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IoTHub.ConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceID;

            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            int deviceDisabledReceivedCount = 0;

            using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, protocol))
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
                Logger.Trace($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Created {nameof(DeviceClient)} with device Id={testDevice.Id}");

                await deviceClient.OpenAsync().ConfigureAwait(false);

                // Receiving the module twin should succeed right now.
                Logger.Trace($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: DeviceClient GetTwinAsync.");
                Shared.Twin twin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(twin);

                // Delete/disable the device in IoT hub. This should trigger the ConnectionStatusChangesHandler.
                using (var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, deviceId).ConfigureAwait(false);
                }

                Logger.Trace($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Completed RegistryManager operation.");

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

                Logger.Trace($"{nameof(DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Asserting connection status change.");

                Assert.AreEqual(1, deviceDisabledReceivedCount);
                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }

        private async Task ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
            Client.TransportType protocol, Func<RegistryManager, string, Task> registryManagerOperation)
        {
            var amqpTransportSettings = new AmqpTransportSettings(protocol);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix, Logger).ConfigureAwait(false);
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

            using (var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings))
            {
                moduleClient.SetConnectionStatusChangesHandler(statusChangeHandler);
                Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Created {nameof(ModuleClient)} with moduleId={testModule.Id}");

                await moduleClient.OpenAsync().ConfigureAwait(false);

                // Receiving the module twin should succeed right now.
                Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: ModuleClient GetTwinAsync.");
                Shared.Twin twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(twin);

                // Delete/disable the device in IoT hub.
                using (var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, testModule.DeviceId).ConfigureAwait(false);
                }

                Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Completed RegistryManager operation.");

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

                Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base)}: Asserting connection status change.");

                Assert.AreEqual(1, deviceDisabledReceivedCount);
                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }
    }
}
