// Copyright (c) Microsoft. All rights reserved.
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
    public class MethodE2EMultiplexingTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MethodE2EMultiplexingTests)}_";
        private readonly int MuxWithoutPoolingDevicesCount = 2;
        private readonly int MuxWithoutPoolingPoolSize = 1; // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        // TODO: #839 - Implement proxy and mux tests
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public MethodE2EMultiplexingTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize, 
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                MethodOperation.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, Task<Task>> setDeviceReceiveMethod
            )
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
                        _log.WriteLine($"{nameof(MethodE2EMultiplexingTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                        deviceClientConnectionStatusChangeCount[deviceClient] = setConnectionStatusChangesHandlerCount;
                    });

                    Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient).ConfigureAwait(false);
                    await Task.WhenAll(
                        MethodOperation.ServiceSendMethodAndVerifyResponse(testDevice.Id),
                        methodReceivedTask).ConfigureAwait(false);
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

                    _log.WriteLine($"{nameof(MethodE2EMultiplexingTests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
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
