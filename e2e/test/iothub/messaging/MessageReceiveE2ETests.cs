// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("LongRunning")]
    public partial class MessageReceiveE2ETests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(MessageReceiveE2ETests)}_";

        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_tenSeconds = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_twentySeconds = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageCancelsAfterSpecifiedDelay_Amqp()
        {
            await DeviceClientGivesUpWaitingForC2dMessageAsync(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageCancelsAfterSpecifiedDelay_Mqtt()
        {
            await DeviceClientGivesUpWaitingForC2dMessageAsync(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallback_Amqp()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallback_Mqtt()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Mqtt()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Amqp()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceivePendingMessageUsingCallback_Mqtt()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceReceivePendingMessageUsingCallback_Amqp()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Mqtt()
        {
            var settings = new ITransportSettings[] { new MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only) { CleanSession = true } };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Amqp()
        {
            var settings = new ITransportSettings[] { new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only) };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Amqp()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        public static (Message message, string payload, string p1Value) ComposeC2dTestMessage(MsTestLogger logger)
        {
            string payload = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeC2dTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, payload, p1Value);
        }

        public static async Task VerifyReceivedC2dMessageAsync(Client.TransportType transport, DeviceClient dc, string deviceId, Message message, string payload, MsTestLogger logger)
        {
            string receivedMessageDestination = $"/devices/{deviceId}/messages/deviceBound";

            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received
                && sw.Elapsed < FaultInjection.RecoveryTime)
            {
                logger.Trace($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                using Client.Message receivedMessage = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);

                receivedMessage.Should().NotBeNull($"No message is received for device {deviceId} in {s_oneMinute}.");

                try
                {
                    // always complete message
                    await dc.CompleteMessageAsync(receivedMessage).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore exception from CompleteAsync
                }

                receivedMessage.MessageId.Should().Be(message.MessageId, "Received message Id is not what was sent by service");
                receivedMessage.UserId.Should().Be(message.UserId, "Received user Id is not what was sent by service");
                receivedMessage.To.Should().Be(receivedMessageDestination, "Received message destination is not what was sent by service");

                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                logger.Trace($"{nameof(VerifyReceivedC2dMessageAsync)}: Received message: for {deviceId}: {messageData}");
                if (Equals(payload, messageData))
                {
                    receivedMessage.Properties.Count.Should().Be(1, $"The count of received properties did not match for device {deviceId}");
                    KeyValuePair<string, string> prop = receivedMessage.Properties.Single();
                    string propertyKey = "property1";
                    prop.Key.Should().Be(propertyKey, $"The key \"property1\" did not match for device {deviceId}");
                    prop.Value.Should().Be(message.Properties[propertyKey], $"The value of \"property1\" did not match for device {deviceId}");
                    received = true;
                }
            }

            sw.Stop();
            received.Should().BeTrue($"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTime}.");
        }

        public static async Task VerifyReceivedC2dMessageWithCancellationTokenAsync(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value, MsTestLogger logger)
        {
            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received
                && sw.Elapsed < FaultInjection.RecoveryTime)
            {
                logger.Trace($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                using Client.Message receivedMessage = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);

                if (receivedMessage == null)
                {
                    Assert.Fail($"No message is received for device {deviceId} in {s_oneMinute}.");
                }

                try
                {
                    // always complete message
                    await dc.CompleteMessageAsync(receivedMessage).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore exception from CompleteAsync
                }

                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                logger.Trace($"{nameof(VerifyReceivedC2dMessageWithCancellationTokenAsync)}: Received message: for {deviceId}: {messageData}");
                if (payload == messageData)
                {
                    receivedMessage.Properties.Count.Should().Be(1, $"The count of received properties did not match for device {deviceId}");
                    KeyValuePair<string, string> prop = receivedMessage.Properties.Single();
                    prop.Key.Should().Be("property1", $"The key \"property1\" did not match for device {deviceId}");
                    prop.Value.Should().Be(p1Value, $"The value of \"property1\" did not match for device {deviceId}");
                    received = true;
                }
            }

            sw.Stop();
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTime}.");
        }

        private async Task DeviceClientGivesUpWaitingForC2dMessageAsync(Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // There is no message being sent so the device client should timeout waiting for the message.

            var delay = TimeSpan.FromSeconds(3);
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(delay);
                await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                sw.Elapsed.Should().BeCloseTo(delay, 1000, $"Cancellation didn't occur near the {delay} specified in the cancellation token.");
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveMessageWithTimeoutAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - calling OpenAsync() for transport={transport}");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            try
            {
                Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - using device client timeout={s_fiveSeconds}");

                await ReceiveMessageWithoutTimeoutCheckAsync(deviceClient, s_oneMinute, s_fiveSeconds, Logger).ConfigureAwait(false);
            }
            finally
            {
                Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - calling CloseAsync() for transport={transport}");
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await serviceClient.OpenAsync().ConfigureAwait(false);

            // For Mqtt - the device needs to have subscribed to the devicebound topic, in order for IoT hub to deliver messages to the device.
            // For this reason we will make a "fake" ReceiveAsync() call, which will result in the device subscribing to the c2d topic.
            // Note: We need this "fake" ReceiveAsync() call even though we (SDK default) CONNECT with a CleanSession flag set to 0.
            // This is because this test device is newly created, and it has never subscribed to IoT hub c2d topic.
            // Hence, IoT hub doesn't know about its CleanSession preference yet.
            if (transport == Client.TransportType.Mqtt
                || transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                using var cts = new CancellationTokenSource(s_oneSecond);
                try
                {
                    using Client.Message discardMessage = await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            }
            await VerifyReceivedC2dMessageWithCancellationTokenAsync(transport, deviceClient, testDevice.Id, payload, p1Value, Logger).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ReceiveMessageWithoutTimeoutCheckAsync(DeviceClient dc, TimeSpan maxTimeToWait, TimeSpan bufferTime, MsTestLogger logger)
        {
            var sw = new Stopwatch();
            logger.Trace($"{nameof(ReceiveMessageWithoutTimeoutCheckAsync)} - Calling ReceiveAsync()");

            using var cts = new CancellationTokenSource(maxTimeToWait);
            sw.Restart();
            try
            {
                using Client.Message message = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                sw.Stop();
                logger.Trace($"{nameof(ReceiveMessageWithoutTimeoutCheckAsync)} - Received message={message}; time taken={sw.Elapsed}.");
                await dc.CompleteMessageAsync(message).ConfigureAwait(false);
                TimeSpan maxLatency = maxTimeToWait + bufferTime;
                sw.Elapsed.Should().BeGreaterThan(maxLatency, $"ReceiveAsync did not return in {maxLatency}; instead it took {sw.Elapsed}.");
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Assert.Fail($"Message not received after {sw.Elapsed}");
            }
        }

        private async Task ReceiveSingleMessageUsingCallbackAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

                using var cts = new CancellationTokenSource(s_tenSeconds);
                Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
                await Task
                    .WhenAll(
                        serviceClient.SendAsync(testDevice.Id, msg),
                        testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token))
                    .ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            // For MQTT - we will need to subscribe to the MQTT receive telemetry topic
            // before the device can begin receiving c2d messages.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                try
                {
                    using var cts1 = new CancellationTokenSource(s_oneSecond);
                    using Client.Message discardMessage = await deviceClient.ReceiveMessageAsync(cts1.Token).ConfigureAwait(false);
                    Logger.Trace($"Leftover message on Mqtt was: {discardMessage} with Id={discardMessage?.MessageId}");
                }
                catch (OperationCanceledException) { }
            }

            // First receive message using the polling ReceiveAsync() API.
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, firstMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={firstMessage.MessageId} - to be received on polling ReceiveAsync");

            using var cts2 = new CancellationTokenSource(s_fiveSeconds);
            using Client.Message receivedFirstMessage = await deviceClient.ReceiveMessageAsync(cts2.Token).ConfigureAwait(false);
            receivedFirstMessage.MessageId.Should().Be(firstMessage.MessageId);
            await deviceClient.CompleteMessageAsync(receivedFirstMessage).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now, send a message to the device from the service.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            testDeviceCallbackHandler.ExpectedMessageSentByService = secondMessage;
            await serviceClient.SendAsync(testDevice.Id, secondMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={secondMessage.MessageId} - to be received on callback");

            // A call to ReceiveAsync() should return null.
            try
            {
                using var cts3 = new CancellationTokenSource(s_fiveSeconds);
                using Client.Message receivedSecondMessage = await deviceClient.ReceiveMessageAsync(cts3.Token).ConfigureAwait(false);
                receivedSecondMessage.Should().BeNull();
            }
            catch (OperationCanceledException) { }

            // The message should be received on the callback
            using var cts4 = new CancellationTokenSource(s_fiveSeconds);
            await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts4.Token).ConfigureAwait(false);

            // Now unsubscribe from receiving c2d messages over the callback.
            await deviceClient.SetReceiveMessageHandlerAsync(null, deviceClient).ConfigureAwait(false);

            // For Mqtt - since we have explicitly unsubscribed, we will need to resubscribe again
            // before the device can begin receiving c2d messages.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                try
                {
                    using var cts5 = new CancellationTokenSource(s_tenSeconds);
                    using Client.Message leftoverMessage = await deviceClient.ReceiveMessageAsync(cts5.Token).ConfigureAwait(false);
                    Logger.Trace($"Leftover message on Mqtt was: {leftoverMessage} with Id={leftoverMessage?.MessageId}");
                }
                catch (OperationCanceledException) { }
            }

            // Send a message to the device from the service.
            (Message thirdMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, thirdMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={thirdMessage.MessageId} - to be received on polling ReceiveAsync");

            // This time, the message should not be received on the callback, rather it should be received on a call to ReceiveAsync().
            using var cts6 = new CancellationTokenSource(s_fiveSeconds);
            Func<Task> receiveMessageOverCallback = async () =>
            {
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts6.Token).ConfigureAwait(false);
            };
            using Client.Message receivedThirdMessage = await deviceClient.ReceiveMessageAsync(cts6.Token).ConfigureAwait(false);
            receivedThirdMessage.MessageId.Should().Be(thirdMessage.MessageId);
            await deviceClient.CompleteMessageAsync(receivedThirdMessage).ConfigureAwait(false);
            receiveMessageOverCallback.Should().Throw<OperationCanceledException>();

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType type, Client.TransportType transport)
        {
            using var firstHandlerSemaphore = new SemaphoreSlim(0, 1);
            using var secondHandlerSemaphore = new SemaphoreSlim(0, 1);

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            // Set the first C2D message handler.
            await deviceClient.SetReceiveMessageHandlerAsync(
                async (message, context) =>
                {
                    Logger.Trace($"Received message over the first message handler: MessageId={message.MessageId}");
                    await deviceClient.CompleteMessageAsync(message).ConfigureAwait(false);
                    firstHandlerSemaphore.Release();
                },
                deviceClient);

            // The C2D message should be received over the first callback handler, releasing the corresponding semaphore.
            using var firstCts = new CancellationTokenSource(s_tenSeconds);
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={firstMessage.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, firstMessage),
                    firstHandlerSemaphore.WaitAsync(firstCts.Token))
                .ConfigureAwait(false);

            // Set the second C2D message handler.
            await deviceClient.SetReceiveMessageHandlerAsync(
                async (message, context) =>
                {
                    Logger.Trace($"Received message over the second message handler: MessageId={message.MessageId}");
                    await deviceClient.CompleteMessageAsync(message).ConfigureAwait(false);
                    secondHandlerSemaphore.Release();
                },
                deviceClient);

            using var secondCts = new CancellationTokenSource(s_tenSeconds);
            Func<Task> secondCallbackHandler = async () =>
            {
                await firstHandlerSemaphore.WaitAsync(secondCts.Token).ConfigureAwait(false);
            };

            // The C2D message should be received over the second callback handler, releasing the corresponding semaphore.
            // The first callback handler should not be called, meaning its semaphore should not be available to be grabbed.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={secondMessage.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, secondMessage),
                    secondHandlerSemaphore.WaitAsync(secondCts.Token))
                .ConfigureAwait(false);
            secondCallbackHandler.Should().Throw<OperationCanceledException>();

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, Client.TransportType transportType)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient1 = testDevice.CreateDeviceClient(transportType);

            // An MQTT client must have connected at least once to be able to receive C2D messages.
            if (transportType == Client.TransportType.Mqtt
                || transportType == Client.TransportType.Mqtt_Tcp_Only
                || transportType == Client.TransportType.Mqtt_WebSocket_Only)
            {
                await deviceClient1.OpenAsync().ConfigureAwait(false);
                await deviceClient1.CloseAsync().ConfigureAwait(false);
            }

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            // Send the message from service.
            (Message msg, string _, string _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            using DeviceClient deviceClient2 = testDevice.CreateDeviceClient(transportType);
            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to false.
            // Also, over MQTT it seems the device must be connected (although not necessarily subscribed for C2D messages)
            // in order for C2D messages to get to the device. If they are offline, the messages will never be delivered.
            await deviceClient2.OpenAsync().ConfigureAwait(false);

            List<Client.Message> receivedMessages = new();
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task OnC2dMessage(Client.Message message, object userContext)
            {
                receivedMessages.Add(message);
                messageReceived.SetResult(true);
                return Task.CompletedTask;
            }

            // After message was sent, subscribe for messages to see if the device can get them.
            await deviceClient2.SetReceiveMessageHandlerAsync(OnC2dMessage, null).ConfigureAwait(false);
            try
            {
                using var cts = new CancellationTokenSource(s_tenSeconds);
                await Task.WhenAny(messageReceived.Task, Task.Delay(-1, cts.Token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            receivedMessages.Should().HaveCount(1);
            receivedMessages.First().MessageId.Should().Be(msg.MessageId);

            await serviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient2.CloseAsync().ConfigureAwait(false);
        }

        private async Task DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, ITransportSettings[] transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings);
            var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now dispose and then reinitialize the client instance.
            await deviceClient.CloseAsync().ConfigureAwait(false);
            deviceClient.Dispose();

            testDeviceCallbackHandler.Dispose();
            testDeviceCallbackHandler = null;

            deviceClient = testDevice.CreateDeviceClient(transportSettings);
            testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to true.
            // This will ensure that messages sent before the device had subscribed to c2d topic are not delivered.
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Send the message from service.
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            // Subscribe to receive C2D messages over the callback.
            testDeviceCallbackHandler.ExpectedMessageSentByService = msg;
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Wait to ensure that the message was not received.
            using var cts = new CancellationTokenSource(s_tenSeconds);
            Func<Task> receiveMessageOverCallback = async () =>
            {
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            };
            receiveMessageOverCallback.Should().Throw<OperationCanceledException>();

            await serviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            deviceClient.Dispose();
            testDeviceCallbackHandler.Dispose();
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType type, Client.TransportType transportType)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType);
            bool lostConnection = false;
            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Disconnected_Retrying)
                {
                    lostConnection = true;
                }
            });

            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // This will make the client unsubscribe from the mqtt c2d topic/close the amqp c2d link. Neither event
            // should close the connection as a whole, though.
            await deviceClient.SetReceiveMessageHandlerAsync(null, null).ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);

            lostConnection.Should().BeFalse();

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
