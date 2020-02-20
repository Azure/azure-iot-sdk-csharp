// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessageReceiveE2EPoolAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2EPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MessageReceiveE2EPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqp(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqp(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

#if !NETCOREAPP1_1 // no support for websockets in amqp

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MultipleConnections(Client.TransportType transportType)
        {
            await ReceiveMessagePoolOverAmqp(
                transportType,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [TestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MultipleConnections(Client.TransportType transportType)
        {
            await ReceiveMessagePoolOverAmqp(
                transportType,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

#endif

        private async Task ReceiveMessagePoolOverAmqp(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var messagesSent = new Dictionary<string, List<string>>();

            // Initialize the service client
            var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                var d2cMessage = MessageReceiveE2ETests.ComposeC2DTestMessage();
                messagesSent.Add(testDevice.Id, new List<string> { d2cMessage.Payload, d2cMessage.P1Value });

                await serviceClient.SendAsync(testDevice.Id, d2cMessage.CloudMessage).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MessageReceiveE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                List<string> msgSent = messagesSent[testDevice.Id];
                string payload = msgSent[0];
                string p1Value = msgSent[1];

                await MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);
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

            await PoolingOverAmqp.TestPoolAmqpAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation,
                authScope,
                true).ConfigureAwait(false);
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
