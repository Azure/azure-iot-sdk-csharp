﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class TwinE2EMultiplexingTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(TwinE2EMultiplexingTests)}_";
        private readonly int MuxWithoutPoolingDevicesCount = 2;
        private readonly int MuxWithoutPoolingPoolSize = 1; // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        // TODO: #839 - Implement proxy and mux tests
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public TwinE2EMultiplexingTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                TwinOperation.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount)
        {
            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transport)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)poolSize),
                        Pooling = true
                    }
                }
            };

            ICollection<DeviceClient> deviceClients = new List<DeviceClient>();
            Dictionary<DeviceClient, int> deviceClientConnectionStatusChangeCount = new Dictionary<DeviceClient, int>();

            try
            {
                _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}: Starting the test execution for {devicesCount} devices");

                for (int i = 0; i < devicesCount; i++)
                {
                    ConnectionStatus? lastConnectionStatus = null;
                    ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
                    int setConnectionStatusChangesHandlerCount = 0;

                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{DevicePrefix}_{i}_").ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                    deviceClients.Add(deviceClient);

                    deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
                    {
                        setConnectionStatusChangesHandlerCount++;
                        lastConnectionStatus = status;
                        lastConnectionStatusChangeReason = statusChangeReason;
                        _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                        deviceClientConnectionStatusChangeCount[deviceClient] = setConnectionStatusChangesHandlerCount;
                    });

                    await TwinOperation.Twin_DeviceSetsReportedPropertyAndGetsItBack(deviceClient).ConfigureAwait(false);
                }
            }
            finally
            {
                // Close and dispose all of the device client instances here
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    // The connection status change count should be 2: connect (open) and disabled (close)
                    Assert.IsTrue(deviceClientConnectionStatusChangeCount[deviceClient] == 2, $"Connection status change count for deviceClient {TestLogging.GetHashCode(deviceClient)} is {deviceClientConnectionStatusChangeCount[deviceClient]}");

                    _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    deviceClient.Dispose();
                }
            }
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync
            )
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transport)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)poolSize),
                        Pooling = true
                    }
                }
            };

            ICollection<DeviceClient> deviceClients = new List<DeviceClient>();
            Dictionary<DeviceClient, int> deviceClientConnectionStatusChangeCount = new Dictionary<DeviceClient, int>();

            try
            {
                _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}: Starting the test execution for {devicesCount} devices");

                for (int i = 0; i < devicesCount; i++)
                {
                    ConnectionStatus? lastConnectionStatus = null;
                    ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
                    int setConnectionStatusChangesHandlerCount = 0;

                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{DevicePrefix}_{i}_").ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                    deviceClients.Add(deviceClient);

                    deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
                    {
                        setConnectionStatusChangesHandlerCount++;
                        lastConnectionStatus = status;
                        lastConnectionStatusChangeReason = statusChangeReason;
                        _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                        deviceClientConnectionStatusChangeCount[deviceClient] = setConnectionStatusChangesHandlerCount;
                    });

                    _log.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp)}: name={propName}, value={propValue}");
                    Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

                    await Task.WhenAll(
                        TwinOperation.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                        updateReceivedTask).ConfigureAwait(false);

                }
            }
            finally
            {
                // Close and dispose all of the device client instances here
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    // The connection status change count should be 2: connect (open) and disabled (close)
                    Assert.IsTrue(deviceClientConnectionStatusChangeCount[deviceClient] == 2, $"Connection status change count for deviceClient {TestLogging.GetHashCode(deviceClient)} is {deviceClientConnectionStatusChangeCount[deviceClient]}");

                    _log.WriteLine($"{nameof(TwinE2EMultiplexingTests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    deviceClient.Dispose();
                }
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
