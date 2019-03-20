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
    [TestCategory("ConnectionPooling_AMQP_E2E")]
    public class MessageSendE2EMultiplexingTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2EMultiplexingTests)}_";
        private readonly int MuxWithoutPoolingDevicesCount = 2;
        // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithoutPoolingPoolSize = 1;
        // These values are configurable
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        // TODO: Implement proxy and mux tests
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageSendE2EMultiplexingTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithoutPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithoutPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_X509_DeviceSendSingleMessage_MuxWithoutPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.X509,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_X509_DeviceSendSingleMessage_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.X509,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_X509_DeviceSendSingleMessage_MuxWithPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.X509,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_X509_DeviceSendSingleMessage_MuxWithPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.X509,
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithoutPoolingInParallel_Amqp()
        //{
        //    await SendMessageMuxedOverAmqpInParallel(
        //        TestDeviceType.Sasl,
        //        ConnectionStringAuthScope.Device,
        //        Client.TransportType.Amqp_Tcp_Only,
        //        MuxWithoutPoolingPoolSize,
        //        MuxDevicesCount
        //        ).ConfigureAwait(false);
        //}

        private async Task SendMessageMuxedOverAmqp(
            TestDeviceType type, 
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

            try
            {
                _log.WriteLine($"{nameof(MessageSendE2ETests)}: Starting the test execution for {devicesCount} devices");

                for (int i = 0; i < devicesCount; i++)
                {
                    DeviceClient deviceClient = null;
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{DevicePrefix}_{i}_", type).ConfigureAwait(false);

                    if (type == TestDeviceType.Sasl)
                    {
                        deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                    }
                    else
                    {
                        deviceClient = testDevice.CreateDeviceClient(transportSettings);
                    }
                    deviceClients.Add(deviceClient);

                    _log.WriteLine($"{nameof(MessageSendE2ETests)}: Preparing to send message for device {i}");
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    await MessageSendUtil.SendSingleMessageAndVerify(deviceClient, testDevice.Id).ConfigureAwait(false);
                }
            }
            finally
            {
                // Close and dispose all of the device client instances here
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                    _log.WriteLine($"{nameof(MessageSendE2ETests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
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
