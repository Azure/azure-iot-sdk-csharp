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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Mqtt()
        {
            var settings = new IotHubClientMqttSettings() { CleanSession = true };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Amqp()
        {
            var settings = new IotHubClientAmqpSettings();
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Amqp()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageAfterOpenCloseOpen_Amqp()
        {
            await ReceiveMessageAfterOpenCloseOpenAsync(TestDeviceType.Sasl, new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageAfterOpenCloseOpen_Mqtt()
        {
            await ReceiveMessageAfterOpenCloseOpenAsync(TestDeviceType.Sasl, new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        public static Message ComposeC2dTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeC2dTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
                Properties = { ["property1"] = p1Value }
            };

            return message;
        }

        public static async Task VerifyReceivedC2dMessageAsync(IotHubDeviceClient dc, string deviceId, Message message, string payload)
        {
            string receivedMessageDestination = $"/devices/{deviceId}/messages/deviceBound";

            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received
                && sw.Elapsed < FaultInjection.RecoveryTime)
            {
                VerboseTestLogger.WriteLine($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                var c2dMessageReceived = new TaskCompletionSource<IncomingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                Func<IncomingMessage, Task<MessageAcknowledgement>> OnC2DMessageReceived = (message) =>
                {
                    c2dMessageReceived.TrySetResult(message);
                    return Task.FromResult(MessageAcknowledgement.Complete);
                };
                await dc.SetIncomingMessageCallbackAsync(OnC2DMessageReceived).ConfigureAwait(false);

                IncomingMessage receivedMessage = await TaskCompletionSourceHelper.GetTaskCompletionSourceResultAsync(c2dMessageReceived, cts.Token).ConfigureAwait(false);

                receivedMessage.MessageId.Should().Be(message.MessageId, "Received message Id is not what was sent by service");
                receivedMessage.UserId.Should().Be(message.UserId, "Received user Id is not what was sent by service");
                receivedMessage.To.Should().Be(receivedMessageDestination, "Received message destination is not what was sent by service");

                bool messageDeserialized = receivedMessage.TryGetPayload(out string messageData);
                messageDeserialized.Should().BeTrue();

                VerboseTestLogger.WriteLine($"{nameof(VerifyReceivedC2dMessageAsync)}: Received message: for {deviceId}: {messageData}");
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

        private async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type).ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

                // Now, set a callback on the device client to receive C2D messages.
                await deviceHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

                // Now, send a message to the device from the service.
                Message firstMsg = ComposeC2dTestMessage(out string _, out string _);
                deviceHandler.ExpectedMessageSentByService = firstMsg;
                await serviceClient.Messages.SendAsync(testDevice.Id, firstMsg).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={firstMsg.MessageId} - to be received on callback");

                // The message should be received on the callback
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await deviceHandler.WaitForReceiveMessageCallbackAsync(cts1.Token).ConfigureAwait(false);

                // Now unsubscribe from receiving c2d messages over the callback.
                await deviceHandler.UnsetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

                // Send a message to the device from the service.
                Message secondMsg = ComposeC2dTestMessage(out string _, out string _);
                await serviceClient.Messages.SendAsync(testDevice.Id, secondMsg).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={secondMsg.MessageId} - to be received on polling ReceiveAsync");

                try
                {
                    using var cts2 = new CancellationTokenSource(s_fiveSeconds);
                    await deviceHandler.WaitForReceiveMessageCallbackAsync(cts2.Token).ConfigureAwait(false);
                    Assert.Fail("Should not have received message over callback.");
                }
                catch (OperationCanceledException) { }
            }
            finally
            {
                await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            Message msg = ComposeC2dTestMessage(out string _, out string _);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now dispose and then reinitialize the client instance.
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.DisposeAsync();

            testDeviceCallbackHandler.Dispose();
            testDeviceCallbackHandler = null;

            deviceClient = testDevice.CreateDeviceClient(options);
            testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);

            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to true.
            // This will ensure that messages sent before the device had subscribed to c2d topic are not delivered.
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Send the message from service.
            VerboseTestLogger.WriteLine($"Sending C2D message from service, messageId={msg.MessageId}");
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
            await receiveMessageOverCallback.Should().ThrowAsync<OperationCanceledException>();

            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            testDeviceCallbackHandler.Dispose();
        }

        private async Task ReceiveMessageAfterOpenCloseOpenAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type).ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

                // Now, set a callback on the device client to receive C2D messages.
                await deviceHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

                // Now, send a message to the device from the service.
                Message testMessage = ComposeC2dTestMessage(out string _, out string _);
                deviceHandler.ExpectedMessageSentByService = testMessage;
                await serviceClient.Messages.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={testMessage.MessageId} - to be received on callback");

                // The message should be received on the callback even though the client was re-opened.
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await deviceHandler.WaitForReceiveMessageCallbackAsync(cts1.Token).ConfigureAwait(false);
            }
            finally
            {
                await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
            }
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            bool lostConnection = false;

            void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
            {
                if (connectionStatusInfo.Status == ConnectionStatus.Disconnected || connectionStatusInfo.Status == ConnectionStatus.DisconnectedRetrying)
                {
                    lostConnection = true;
                }
            }
            deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;

            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // This will make the client unsubscribe from the mqtt c2d topic/close the amqp c2d link. Neither event
            // should close the connection as a whole, though.
            await deviceClient.SetIncomingMessageCallbackAsync(null).ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);

            lostConnection.Should().BeFalse();

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
