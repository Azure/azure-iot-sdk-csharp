// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading.Tasks;

    [TestClass]
    [TestCategory("IoTHub-E2E")]
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
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_Tcp_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // Re-enable test once PR #1102 is checked-in.
        // [TestMethod]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_Tcp_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        // Re-enable test once PR #1102 is checked-in.
        // [TestMethod]
        public async Task DeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_WebSocket_Only, async (r, d) =>
                {
                    Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.UpdateDeviceAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_WebSocket_Only, async (r, d) => await r.RemoveDeviceAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // IoT Hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [TestMethod]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
                TransportType.Amqp_Tcp_Only, async (r, d) =>
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
                TransportType.Amqp_WebSocket_Only, async (r, d) =>
            {
                Device device = await r.GetDeviceAsync(d).ConfigureAwait(false);
                device.Status = DeviceStatus.Disabled;
                await r.UpdateDeviceAsync(device).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        async Task DeviceClient_Gives_ConnectionStatus_DeviceDisabled_Base(
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

                // Delete the device in IoT Hub. This should trigger the ConnectionStatusChangesHandler.
                using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, deviceId).ConfigureAwait(false);
                }

                // Periodically keep retrieving the device twin to keep connection alive.
                // The ConnectionStatusChangesHandler should be triggered when the connection is closed from IoT hub with an
                // exception thrown.
                int twinRetrievals = 50;
                for (int i = 0; i < twinRetrievals; i++)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                        if (deviceDisabledReceivedCount == 1)
                        {
                            // Call an API on the client again to trigger the ConnectionStatusChangesHandler once again with the 
                            // Device_Disabled status.
                            // This currently does not work due to some issues with IoT hub allowing new connections even when the
                            // device is deleted/disabled. Once that problem is investigated and fixed, we can re-enable this call
                            // and test for multiple invocations of the ConnectionStatusChangeHandler.
                            // await deviceClient.GetTwinAsync().ConfigureAwait(false);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLine($"Exception occurred while retrieving module twin: {ex}");
                    }
                }

                Assert.AreEqual(1, deviceDisabledReceivedCount);
                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }

        async Task ModuleClient_Gives_ConnectionStatus_DeviceDisabled_Base(
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

                // Delete the device in IoT Hub.
                using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                {
                    await registryManagerOperation(registryManager, testModule.DeviceId).ConfigureAwait(false);
                }

                // Periodically keep retrieving the device twin to keep connection alive.
                // The ConnectionStatusChangesHandler should be triggered when the connection is closed from IoT hub with an
                // exception thrown.
                int twinRetrievals = 50;
                for (int i = 0; i < twinRetrievals; i++)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                        if (deviceDisabledReceivedCount == 1)
                        {
                            // Call an API on the client again to trigger the ConnectionStatusChangesHandler once again with the 
                            // Device_Disabled status.
                            // This currently does not work due to some issues with IoT hub allowing new connections even when the
                            // device is deleted/disabled. Once that problem is investigated and fixed, we can re-enable this call
                            // and test for multiple invocations of the ConnectionStatusChangeHandler.
                            // await moduleClient.GetTwinAsync().ConfigureAwait(false);
                            break;
                        }
                    }
                    catch (IotHubException ex)
                    {
                        _log.WriteLine($"Exception occurred while retrieving module twin: {ex}");
                        Assert.IsInstanceOfType(ex.InnerException, typeof(DeviceNotFoundException));
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
