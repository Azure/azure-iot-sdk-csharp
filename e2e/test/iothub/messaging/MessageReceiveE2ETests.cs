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
using Microsoft.Azure.Devices.E2ETests.helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Rest;
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
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
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

                var c2dMessageReceived = new TaskCompletionSource<Client.Message>(TaskCreationOptions.RunContinuationsAsynchronously);
                Func<Client.Message, object, Task<MessageAcknowledgementType>> OnC2DMessageReceived = (message, context) =>
                {
                    c2dMessageReceived.SetResult(message);
                    return Task.FromResult(MessageAcknowledgementType.Complete);
                };
                await dc.SetReceiveMessageHandlerAsync(OnC2DMessageReceived, null).ConfigureAwait(false);

                Client.Message receivedMessage = await TaskCompletionSourceHelper.GetTaskCompletionSourceResultAsync(c2dMessageReceived, cts.Token).ConfigureAwait(false);

                receivedMessage.Should().NotBeNull($"No message is received for device {deviceId} in {s_oneMinute}.");

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

        private async Task ReceiveSingleMessageUsingCallbackAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

            using var cts = new CancellationTokenSource(s_tenSeconds);
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            await Task
                .WhenAll(
                    serviceClient.Messages.SendAsync(testDevice.Id, msg),
                    testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token))
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
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
                    Func<Client.Message, object, Task<MessageAcknowledgementType>> OnC2DMessageReceived = (message, context) =>
                    {
                        return Task.FromResult(MessageAcknowledgementType.Complete);
                    };
                    await deviceClient1.SetReceiveMessageHandlerAsync(OnC2DMessageReceived, null).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                await deviceClient1.CloseAsync().ConfigureAwait(false);
            }

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            // Send the message from service.
            (Message msg, string _, string _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messages.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            using IotHubDeviceClient deviceClient2 = testDevice.CreateDeviceClient(options);
            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to false.
            // Also, over MQTT it seems the device must be connected (although not necessarily subscribed for C2D messages)
            // in order for C2D messages to get to the device. If they are offline, the messages will never be delivered.
            await deviceClient2.OpenAsync().ConfigureAwait(false);

            List<string> receivedMessageIds = new();
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<MessageAcknowledgementType> OnC2dMessageAsync(Client.Message message, object userContext)
            {
                receivedMessageIds.Add(message.MessageId);
                messageReceived.SetResult(true);
                return Task.FromResult(MessageAcknowledgementType.Complete);
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

            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            await deviceClient2.CloseAsync().ConfigureAwait(false);
        }

        private async Task DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

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
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messages.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

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

            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            deviceClient.Dispose();
            testDeviceCallbackHandler.Dispose();
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            bool lostConnection = false;
            deviceClient.SetConnectionStatusChangeHandler(connectionStatusInfo =>
            {
                if (connectionStatusInfo.Status == ConnectionStatus.Disconnected || connectionStatusInfo.Status == ConnectionStatus.DisconnectedRetrying)
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
