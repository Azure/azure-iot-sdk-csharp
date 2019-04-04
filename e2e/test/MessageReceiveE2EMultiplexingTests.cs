﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
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
    public class MessageReceiveE2EMultiplexingTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2EMultiplexingTests)}_";
        private readonly int MuxWithoutPoolingDevicesCount = 2;
        private readonly int MuxWithoutPoolingPoolSize = 1; // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        // TODO: #839 - Implement proxy and mux tests
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageReceiveE2EMultiplexingTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        private async Task ReceiveMessageMuxedOverAmqp(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
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
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            try
            {
                _log.WriteLine($"{nameof(MessageReceiveE2EMultiplexingTests)}: Starting the test execution for {devicesCount} devices");
                await serviceClient.OpenAsync().ConfigureAwait(false);

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
                        _log.WriteLine($"{nameof(MessageReceiveE2EMultiplexingTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                        deviceClientConnectionStatusChangeCount[deviceClient] = setConnectionStatusChangesHandlerCount;
                    });

                    _log.WriteLine($"{nameof(MessageReceiveE2EMultiplexingTests)}: Preparing to receive message for device {i}");
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    string payload, messageId, p1Value;

                    Message msg = MessageReceive.ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                    await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                    await MessageReceive.VerifyReceivedC2DMessageAsync(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
                }
            }
            finally
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();

                // Close and dispose all of the device client instances here
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    // The connection status change count should be 2: connect (open) and disabled (close)
                    Assert.IsTrue(deviceClientConnectionStatusChangeCount[deviceClient] == 2);

                    _log.WriteLine($"{nameof(MessageReceiveE2EMultiplexingTests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
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
