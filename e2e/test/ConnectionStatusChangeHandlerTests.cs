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
            await this.DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_TCP()
        {
            await this.ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AMQP_WS()
        {
            await this.ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        async Task DeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(Client.TransportType protocol)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new Configuration.IoTHub.DeviceConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceID;

            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            bool deviceDisabledReceived = false;

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, protocol))
            {
                ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
                {
                    if (r == ConnectionStatusChangeReason.Device_Disabled)
                    {
                        status = s;
                        statusChangeReason = r;
                        deviceDisabledReceived = true;
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
                    await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                }

                // Periodically keep retrieving the device twin to keep connection alive.
                // The ConnectionStatusChangesHandler should be triggered when the connection is closed from IoT hub with an
                // exception thrown.
                int twinRetrievals = 10;
                for (int i = 0; i < twinRetrievals; i++)
                {
                    try
                    {
                        if (deviceDisabledReceived)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                        await deviceClient.GetTwinAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _log.WriteLine($"Exception occurred while retrieving module twin: {ex}");
                    }
                }

                Assert.AreEqual(ConnectionStatus.Disconnected, status);
                Assert.AreEqual(ConnectionStatusChangeReason.Device_Disabled, statusChangeReason);
            }
        }

        async Task ModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_Base(Client.TransportType protocol)
        {
            AmqpTransportSettings amqpTransportSettings = new AmqpTransportSettings(protocol);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix).ConfigureAwait(false);
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            bool deviceDisabledReceived = false;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                if (r == ConnectionStatusChangeReason.Device_Disabled)
                {
                    status = s;
                    statusChangeReason = r;
                    deviceDisabledReceived = true;
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
                    await registryManager.RemoveDeviceAsync(testModule.DeviceId).ConfigureAwait(false);
                }

                // Periodically keep retrieving the device twin to keep connection alive.
                // The ConnectionStatusChangesHandler should be triggered when the connection is closed from IoT hub with an
                // exception thrown.
                int twinRetrievals = 10;
                for (int i = 0; i < twinRetrievals; i++)
                {
                    try
                    {
                        if (deviceDisabledReceived)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                        await moduleClient.GetTwinAsync().ConfigureAwait(false);
                    }
                    catch (IotHubException ex)
                    {
                        _log.WriteLine($"Exception occurred while retrieving module twin: {ex}");
                        Assert.IsInstanceOfType(ex.InnerException, typeof(DeviceNotFoundException));
                    }
                }

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
