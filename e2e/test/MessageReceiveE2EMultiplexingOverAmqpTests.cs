// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class MessageReceiveE2EMultiplexingOverAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2EMultiplexingOverAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MessageReceiveE2EMultiplexingOverAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MuxedWithPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithoutPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithPooling_Amqp()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MuxedWithPooling_AmqpWs()
        {
            await ReceiveMessageMuxedOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        private async Task ReceiveMessageMuxedOverAmqp(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Dictionary<string, List<string>> messagesSent = new Dictionary<string, List<string>>();

            // Initialize the service client
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            string payload, messageId, p1Value;

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                Message msg = MessageReceiveE2ETests.ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                messagesSent.Add(testDevice.Id, new List<string> { payload, p1Value });

                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MessageReceiveE2EMultiplexingOverAmqpTests)}: Preparing to receive message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                List<string> msgSent = messagesSent[testDevice.Id];
                payload = msgSent[0];
                p1Value = msgSent[1];

                await MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();

                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }

                messagesSent.Clear();
            };

            await MultiplexingOverAmqp.TestMultiplexingOperationAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation
                ).ConfigureAwait(false);
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
