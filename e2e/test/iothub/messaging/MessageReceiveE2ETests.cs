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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageCancelsAfterSpecifiedDelay_Amqp()
        {
            await IotHubDeviceClient_GivesUpWaitingForC2dMessageAsync(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageCancelsAfterSpecifiedDelay_Mqtt()
        {
            await IotHubDeviceClient_GivesUpWaitingForC2dMessageAsync(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_Amqp()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_Mqtt()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Mqtt()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Amqp()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_Mqtt()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_Amqp()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Mqtt()
        {
            var settings = new IotHubClientMqttSettings() { CleanSession = true };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Amqp()
        {
            var settings = new IotHubClientAmqpSettings();
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Amqp()
        {
            await UnsubscribeDoesNotCauseConnectionStateEventAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            await UnsubscribeDoesNotCauseConnectionStateEventAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
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

        public static async Task VerifyReceivedC2dMessageAsync(IotHubDeviceClient dc, string deviceId, Message message, string payload, MsTestLogger logger)
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
                Client.Message receivedMessage = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);

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

                string messageData = Encoding.ASCII.GetString(receivedMessage.Payload);
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

        public static async Task VerifyReceivedC2dMessageWithCancellationTokenAsync(
            IotHubDeviceClient dc,
            string deviceId,
            string payload,
            string p1Value,
            MsTestLogger logger)
        {
            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received
                && sw.Elapsed < FaultInjection.RecoveryTime)
            {
                logger.Trace($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                Client.Message receivedMessage = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);

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

                string messageData = Encoding.ASCII.GetString(receivedMessage.Payload);
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

        private async Task IotHubDeviceClient_GivesUpWaitingForC2dMessageAsync(IotHubClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));

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

        private async Task ReceiveMessageWithTimeoutAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            string transportInfo = $"{transportSettings.GetType()}/{transportSettings.Protocol}";
            Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - calling OpenAsync() for transport={transportInfo}");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            try
            {
                Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - using device client timeout={s_fiveSeconds}");

                await ReceiveMessageWithoutTimeoutCheckAsync(deviceClient, s_oneMinute, s_fiveSeconds, Logger).ConfigureAwait(false);
            }
            finally
            {
                Logger.Trace($"{nameof(ReceiveMessageWithTimeoutAsync)} - calling CloseAsync() for transport={transportInfo}");
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await serviceClient.OpenAsync().ConfigureAwait(false);

            // For Mqtt - the device needs to have subscribed to the devicebound topic, in order for IoT hub to deliver messages to the device.
            // For this reason we will make a "fake" ReceiveAsync() call, which will result in the device subscribing to the c2d topic.
            // Note: We need this "fake" ReceiveAsync() call even though we (SDK default) CONNECT with a CleanSession flag set to 0.
            // This is because this test device is newly created, and it has never subscribed to IoT hub c2d topic.
            // Hence, IoT hub doesn't know about its CleanSession preference yet.
            if (transportSettings is IotHubClientMqttSettings)
            {
                using var cts = new CancellationTokenSource(s_oneSecond);
                try
                {
                    Client.Message discardMessage = await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            await VerifyReceivedC2dMessageWithCancellationTokenAsync(deviceClient, testDevice.Id, payload, p1Value, Logger).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ReceiveMessageWithoutTimeoutCheckAsync(IotHubDeviceClient dc, TimeSpan maxTimeToWait, TimeSpan bufferTime, MsTestLogger logger)
        {
            var sw = new Stopwatch();
            logger.Trace($"{nameof(ReceiveMessageWithoutTimeoutCheckAsync)} - Calling ReceiveAsync()");

            using var cts = new CancellationTokenSource(maxTimeToWait);
            sw.Restart();
            try
            {
                Client.Message message = await dc.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
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

        private async Task ReceiveSingleMessageUsingCallbackAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

            using var cts = new CancellationTokenSource(s_tenSeconds);
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, msg),
                    testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token))
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            // For MQTT - we will need to subscribe to the MQTT receive telemetry topic
            // before the device can begin receiving c2d messages.
            if (transportSettings is IotHubClientMqttSettings)
            {
                try
                {
                    using var cts1 = new CancellationTokenSource(s_oneSecond);
                    Client.Message discardMessage = await deviceClient.ReceiveMessageAsync(cts1.Token).ConfigureAwait(false);
                    Logger.Trace($"Leftover message on Mqtt was: {discardMessage} with Id={discardMessage?.MessageId}");
                }
                catch (OperationCanceledException) { }
            }

            // First receive message using the polling ReceiveAsync() API.
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, firstMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={firstMessage.MessageId} - to be received on polling ReceiveAsync");

            using var cts2 = new CancellationTokenSource(s_fiveSeconds);
            Client.Message firstPolledMessage = await deviceClient.ReceiveMessageAsync(cts2.Token).ConfigureAwait(false);
            firstPolledMessage.MessageId.Should().Be(firstMessage.MessageId);
            await deviceClient.CompleteMessageAsync(firstPolledMessage).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now, send a message to the device from the service.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            deviceHandler.ExpectedMessageSentByService = secondMessage;
            await serviceClient.SendAsync(testDevice.Id, secondMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={secondMessage.MessageId} - to be received on callback");

            // A call to ReceiveAsync() should return null immediately because the client has a subscription.
            using var cts3 = new CancellationTokenSource(s_fiveSeconds);
            Client.Message noMessageShouldBeDelivered = await deviceClient.ReceiveMessageAsync(cts3.Token).ConfigureAwait(false);
            noMessageShouldBeDelivered.Should().BeNull();

            // The message should be received on the callback
            await deviceHandler.WaitForReceiveMessageCallbackAsync(cts3.Token).ConfigureAwait(false);

            // Now unsubscribe from receiving c2d messages over the callback.
            await deviceHandler.UnSetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // For Mqtt - since we have explicitly unsubscribed, we will need to resubscribe again
            // before the device can begin receiving c2d messages.
            if (transportSettings is IotHubClientMqttSettings)
            {
                try
                {
                    using var cts4 = new CancellationTokenSource(s_oneSecond);
                    Client.Message unexpectedLeftoverMessage = await deviceClient.ReceiveMessageAsync(cts4.Token).ConfigureAwait(false);
                    Logger.Trace($"Leftover message on Mqtt was: {unexpectedLeftoverMessage} with Id={unexpectedLeftoverMessage?.MessageId}");
                    await deviceClient.CompleteMessageAsync(unexpectedLeftoverMessage).ConfigureAwait(false);
                    unexpectedLeftoverMessage.Should().BeNull("Didn't expect to receive a message by polling when none was sent for this scenario.");
                }
                catch (OperationCanceledException) { }
            }

            // Send a message to the device from the service.
            (Message thirdMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, thirdMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={thirdMessage.MessageId} - to be received on polling ReceiveAsync");

            // This time, the message should not be received on the callback, rather it should be received on a call to ReceiveAsync().
            try
            {
                using var cts5 = new CancellationTokenSource(s_oneSecond);
                await deviceHandler.WaitForReceiveMessageCallbackAsync(cts5.Token).ConfigureAwait(false);
                Assert.Fail("Should not have received message over callback.");
            }
            catch (OperationCanceledException) { }

            using var cts6 = new CancellationTokenSource(s_fiveSeconds);
            Client.Message secondPolledMessage = await deviceClient.ReceiveMessageAsync(cts6.Token).ConfigureAwait(false);
            secondPolledMessage.MessageId.Should().Be(thirdMessage.MessageId);
            await deviceClient.CompleteMessageAsync(secondPolledMessage).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var deviceHandler1 = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            // Set the first C2D message handler.
            await deviceHandler1.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // The C2D message should be received over the first callback handler, releasing the corresponding semaphore.
            using var cts1 = new CancellationTokenSource(s_tenSeconds);
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            deviceHandler1.ExpectedMessageSentByService = firstMessage;
            Logger.Trace($"Sending C2D message from service, messageId={firstMessage.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, firstMessage),
                    deviceHandler1.WaitForReceiveMessageCallbackAsync(cts1.Token))
                .ConfigureAwait(false);

            // Set the second C2D message handler.
            using var deviceHandler2 = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
            await deviceHandler2.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            using var cts2 = new CancellationTokenSource(s_tenSeconds);
            Func<Task> formerCallbackHandler = async () =>
            {
                await deviceHandler1.WaitForReceiveMessageCallbackAsync(cts2.Token).ConfigureAwait(false);
            };

            // The C2D message should be received over the second callback handler, releasing the corresponding semaphore.
            // The first callback handler should not be called, meaning its semaphore should not be available to be grabbed.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            deviceHandler2.ExpectedMessageSentByService = secondMessage;
            Logger.Trace($"Sending C2D message from service, messageId={secondMessage.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, secondMessage),
                    deviceHandler2.WaitForReceiveMessageCallbackAsync(cts2.Token))
                .ConfigureAwait(false);
            await formerCallbackHandler.Should().ThrowAsync<OperationCanceledException>();

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using IotHubDeviceClient deviceClient1 = testDevice.CreateDeviceClient(options);

            // An MQTT client must have connected at least once to be able to receive C2D messages.
            if (transportSettings is IotHubClientMqttSettings)
            {
                await deviceClient1.OpenAsync().ConfigureAwait(false);
                try
                {
                    using var cts = new CancellationTokenSource(s_oneSecond);
                    Client.Message noExpectedMsg = await deviceClient1.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                    await deviceClient1.CompleteMessageAsync(noExpectedMsg).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                await deviceClient1.CloseAsync().ConfigureAwait(false);
            }

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            // Send the message from service.
            (Message msg, string _, string _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            using IotHubDeviceClient deviceClient2 = testDevice.CreateDeviceClient(options);
            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to false.
            // Also, over MQTT it seems the device must be connected (although not necessarily subscribed for C2D messages)
            // in order for C2D messages to get to the device. If they are offline, the messages will never be delivered.
            await deviceClient2.OpenAsync().ConfigureAwait(false);

            List<string> receivedMessageIds = new();
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task OnC2dMessageAsync(Client.Message message, object userContext)
            {
                receivedMessageIds.Add(message.MessageId);
                await deviceClient2.CompleteMessageAsync(message).ConfigureAwait(false);

                messageReceived.SetResult(true);
            }

            // After message was sent, subscribe for messages to see if the device can get them.
            await deviceClient2.SetReceiveMessageHandlerAsync(OnC2dMessageAsync, null).ConfigureAwait(false);
            try
            {
                using var cts = new CancellationTokenSource(s_tenSeconds);
                await Task.WhenAny(messageReceived.Task, Task.Delay(-1, cts.Token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            receivedMessageIds.Should().HaveCount(1);
            receivedMessageIds.First().Should().Be(msg.MessageId);

            await serviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient2.CloseAsync().ConfigureAwait(false);
        }

        private async Task DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
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

            deviceClient = testDevice.CreateDeviceClient(options);
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
        private async Task UnsubscribeDoesNotCauseConnectionStateEventAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            bool lostConnection = false;
            deviceClient.SetConnectionStateChangeHandler(connectionInfo =>
            {
                if (connectionInfo.State == ConnectionState.Disconnected || connectionInfo.State == ConnectionState.DisconnectedRetrying)
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
