// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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

        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_SingleConnection_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MultipleConnections_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessage_MultipleConnections_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MultipleConnections_Amqp()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessage_MultipleConnections_AmqpWs()
        {
            await ReceiveMessagePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackPoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackPoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackPoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallback_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackPoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribePoolOverAmqpAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_DeviceReceiveSingleMessageUsingCallbackAndUnsubscribe_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribePoolOverAmqpAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessagePoolOverAmqpAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var messagesSent = new Dictionary<string, Tuple<Message, string>>();

            // Initialize the service client
            var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                messagesSent.Add(testDevice.Id, Tuple.Create(msg, payload));

                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                Logger.Trace($"{nameof(MessageReceiveE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                Tuple<Message, string> msgSent = messagesSent[testDevice.Id];
                await MessageReceiveE2ETests.VerifyReceivedC2dMessageAsync(transport, deviceClient, testDevice.Id, msgSent.Item1, msgSent.Item2, Logger).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();
                messagesSent.Clear();
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackPoolOverAmqpAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            // Initialize the service client
            var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);

                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                Logger.Trace($"{nameof(MessageReceiveE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackAndUnsubscribePoolOverAmqpAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                var generalTimeout = TimeSpan.FromSeconds(20);

                // Send a message to the device from the service.
                (Message firstMessage, string payload, _) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                testDeviceCallbackHandler.ExpectedMessageSentByService = firstMessage;
                await serviceClient.SendAsync(testDevice.Id, firstMessage).ConfigureAwait(false);
                Logger.Trace($"Sent 1st C2D message from service - to be received on callback: deviceId={testDevice.Id}, messageId={firstMessage.MessageId}");

                // The message should be received on the callback, while a call to ReceiveMessageAsync() should return null.
                using var ctsReceive1 = new CancellationTokenSource(generalTimeout);
                Client.Message receivedMessage = null;
                try
                {
                    receivedMessage = await deviceClient.ReceiveMessageAsync(ctsReceive1.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                receivedMessage.Should().BeNull();

                using var ctsCallback1 = new CancellationTokenSource(generalTimeout);
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(ctsCallback1.Token).ConfigureAwait(false);

                // Now unsubscribe from receiving c2d messages over the callback.
                using var ctsUnsub = new CancellationTokenSource(generalTimeout);
                await deviceClient.SetReceiveMessageHandlerAsync(null, deviceClient, ctsUnsub.Token).ConfigureAwait(false);

                // Send a message to the device from the service.
                (Message secondMessage, _, _) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                await serviceClient.SendAsync(testDevice.Id, secondMessage).ConfigureAwait(false);
                Logger.Trace($"Sent 2nd C2D message from service - to be received on polling ReceiveMessageAsync(): deviceId={testDevice.Id}, messageId={secondMessage.MessageId}");

                // This time, the message should not be received on the callback, rather it should be received on a call to ReceiveMessageAsync().
                using var ctsCallback2 = new CancellationTokenSource();
                Task callbackTask = testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(ctsCallback2.Token);

                using var ctsReceive2 = new CancellationTokenSource(generalTimeout);
                using Client.Message polledMessage2 = await deviceClient.ReceiveMessageAsync(ctsReceive2.Token).ConfigureAwait(false);
                polledMessage2.MessageId.Should().Be(secondMessage.MessageId);
                ctsCallback2.Cancel();

                try
                {
                    await callbackTask.ConfigureAwait(false);
                    Assert.Fail("Callback task should have thrown.");
                }
                catch (OperationCanceledException) { }
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    true,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
