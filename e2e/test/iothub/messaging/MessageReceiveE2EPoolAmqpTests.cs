// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessageReceiveE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2EPoolAmqpTests)}_";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MultipleConnections_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MultipleConnections_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MultipleConnections_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MultipleConnections_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task ReceiveMessagePoolOverAmqpAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var messagesSent = new Dictionary<string, Tuple<Message, string>>();

            // Initialize the service client
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                messagesSent.Add(testDevice.Id, Tuple.Create(msg, payload));

                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                Logger.Trace($"{nameof(MessageReceiveE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                Tuple<Message, string> msgSent = messagesSent[testDevice.Id];
                await MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, msgSent.Item1, msgSent.Item2, Logger).ConfigureAwait(false);
            };

            Func<Task> cleanupOperation = async () =>
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();
                messagesSent.Clear();
            };

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    initOperation,
                    testOperation,
                    cleanupOperation,
                    authScope,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
