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
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessageSendE2EPoolAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2EPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MessageSendE2EPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_Amqp()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_SingleConnection_Amqp()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_SingleConnection_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MultipleConnections_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task SendMessagePoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MessageSendE2EPoolAmqpTests)}: Preparing to send message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                (Client.Message testMessage, string messageId, string payload, string p1Value) = MessageSendE2ETests.ComposeD2CTestMessage();
                _log.WriteLine($"{nameof(MessageSendE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(testDevice.Id, payload, p1Value);
                Assert.IsTrue(isReceived, "Message is not received.");
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
            };

            await PoolingOverAmqp.TestPoolAmqpAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                (d, t) => { return Task.FromResult(false); },
                testOperation,
                cleanupOperation,
                authScope).ConfigureAwait(false);
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
