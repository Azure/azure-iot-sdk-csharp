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
    public class MessageSendE2EPoolAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2EPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static readonly TestLogging _log = TestLogging.GetInstance();

        public MessageSendE2EPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
#if! NETCOREAPP1_1
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
#endif
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection(Client.TransportType transportType)
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    transportType,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
#if! NETCOREAPP1_1

        [Ignore]
        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_SingleConnection(Client.TransportType transportType)
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    transportType,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections(Client.TransportType transportType)
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    transportType,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MultipleConnections(Client.TransportType transportType)
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    transportType,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

#endif

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

                var d2cMessage = MessageSendE2ETests.ComposeD2CTestMessage();
                _log.WriteLine($"{nameof(MessageSendE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{d2cMessage.MessageId}' payload='{d2cMessage.Payload}' p1Value='{d2cMessage.P1Value}'");
                await deviceClient.SendEventAsync(d2cMessage.ClientMessage).ConfigureAwait(false);

                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(testDevice.Id, d2cMessage.Payload, d2cMessage.P1Value);
                Assert.IsTrue(isReceived, "Message is not received.");
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
            };

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    (d, t) => Task.FromResult(false),
                    testOperation,
                    cleanupOperation,
                    authScope,
                    true)
                .ConfigureAwait(false);
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
