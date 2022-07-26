// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    public class MessageSendE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(MessageSendE2EPoolAmqpTests)}_";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_Amqp()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_SingleConnection_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_SingleConnection_Amqp()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_SingleConnection_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MultipleConnections_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MultipleConnections_Amqp()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MultipleConnections_AmqpWs()
        {
            await SendMessagePoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task SendMessagePoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                Logger.Trace($"{nameof(MessageSendE2EPoolAmqpTests)}: Preparing to send message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                (Client.Message testMessage, string payload, string p1Value) = MessageSendE2ETests.ComposeD2cTestMessage(Logger);
                Logger.Trace($"{nameof(MessageSendE2EPoolAmqpTests)}.{testDevice.Id}: messageId='{testMessage.MessageId}' payload='{payload}' p1Value='{p1Value}'");
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    null,
                    TestOperationAsync,
                    null,
                    authScope,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
