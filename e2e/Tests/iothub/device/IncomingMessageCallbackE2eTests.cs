﻿// Copyright (c) Microsoft. All rights reserved.
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
    [TestCategory("IoTHub-Client")]
    [TestCategory("LongRunning")]
    public class IncomingMessageCallbackE2eTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(IncomingMessageCallbackE2eTests)}_";

        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_tenSeconds = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_twentySeconds = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(30);

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

        public static async Task VerifyReceivedC2dMessageAsync(IotHubDeviceClient dc, string deviceId, OutgoingMessage message, string payload)
        {
            string receivedMessageDestination = $"/devices/{deviceId}/messages/deviceBound";

            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received
                && sw.Elapsed < FaultInjection.s_recoveryTime)
            {
                VerboseTestLogger.WriteLine($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                var c2dMessageReceived = new TaskCompletionSource<IncomingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<MessageAcknowledgement> OnC2DMessageReceived(IncomingMessage message)
                {
                    c2dMessageReceived.TrySetResult(message);
                    return Task.FromResult(MessageAcknowledgement.Complete);
                }
                await dc.SetIncomingMessageCallbackAsync(OnC2DMessageReceived).ConfigureAwait(false);

                IncomingMessage receivedMessage = await c2dMessageReceived.WaitAsync(cts.Token).ConfigureAwait(false);

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
            received.Should().BeTrue($"No message received for device {deviceId} with payload={payload} in {FaultInjection.s_recoveryTime}.");
        }

        private static async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using var createTestDeviceCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await testDevice.OpenWithRetryAsync(openCts.Token).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>().ConfigureAwait(false);

            // Now, send a message to the device from the service.
            OutgoingMessage firstMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = firstMsg;
            await serviceClient.Messages.SendAsync(testDevice.Id, firstMsg).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={firstMsg.MessageId} - to be received on callback");

            // The message should be received on the callback
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await deviceHandler.WaitForIncomingMessageCallbackAsync(cts1.Token).ConfigureAwait(false);

            // Now unsubscribe from receiving c2d messages over the callback.
            await deviceHandler.UnsetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Send a message to the device from the service.
            OutgoingMessage secondMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            await serviceClient.Messages.SendAsync(testDevice.Id, secondMsg).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={secondMsg.MessageId} - which should not be received.");

            try
            {
                using var cts2 = new CancellationTokenSource(s_fiveSeconds);
                await deviceHandler.WaitForIncomingMessageCallbackAsync(cts2.Token).ConfigureAwait(false);
                Assert.Fail("Should not have received message over callback.");
            }
            catch (OperationCanceledException) { }
        }

        private static async Task ReceiveMessageAfterOpenCloseOpenAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using var createTestDeviceCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type, createTestDeviceCts.Token).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await testDevice.OpenWithRetryAsync(openCts.Token).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            // Close and re-open the client under test.
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>().ConfigureAwait(false);

            // Now, send a message to the device from the service.
            OutgoingMessage testMessage = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = testMessage;
            await serviceClient.Messages.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={testMessage.MessageId} - to be received on callback");

            // The message should be received on the callback even though the client was re-opened.
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await deviceHandler.WaitForIncomingMessageCallbackAsync(cts1.Token).ConfigureAwait(false);
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings)
        {
            using var createTestDeviceCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, type, createTestDeviceCts.Token).ConfigureAwait(false);
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

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>().ConfigureAwait(false);

            // This will make the client unsubscribe from the mqtt c2d topic/close the amqp c2d link. Neither event
            // should close the connection as a whole, though.
            await deviceClient.SetIncomingMessageCallbackAsync(null).ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);

            lostConnection.Should().BeFalse();
        }
    }
}
