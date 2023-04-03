// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class IncomingMessageCallbackE2eTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(IncomingMessageCallbackE2eTests)}_";

        [TestMethod]
        [TestCategory("LongRunning")]   // This test takes the complete 3 minutes before exiting. So, we will mark this as "LongRunning" so that it doesn't run at PR gate.
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(new IotHubClientAmqpSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]   // This test takes the complete 3 minutes before exiting. So, we will mark this as "LongRunning" so that it doesn't run at PR gate.
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(new IotHubClientMqttSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(new IotHubClientAmqpSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(new IotHubClientMqttSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceReceiveMessageAfterOpenCloseOpen_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageAfterOpenCloseOpenAsync(new IotHubClientAmqpSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceReceiveMessageAfterOpenCloseOpen_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageAfterOpenCloseOpenAsync(new IotHubClientMqttSettings(), ct).ConfigureAwait(false);
        }

        private static async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, ct: ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            OutgoingMessage firstMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = firstMsg;
            await serviceClient.Messages.SendAsync(testDevice.Id, firstMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={firstMsg.MessageId} - to be received on callback");

            await deviceHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);

            // Now unsubscribe from receiving c2d messages over the callback.
            await deviceHandler.UnsetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Send a message to the device from the service.
            OutgoingMessage secondMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            await serviceClient.Messages.SendAsync(testDevice.Id, secondMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={secondMsg.MessageId} - which should not be received.");

            Func<Task> receiveMessageOverCallback = async () =>
            {
                await deviceHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
            };
            await receiveMessageOverCallback.Should().ThrowAsync<OperationCanceledException>();
        }

        private static async Task ReceiveMessageAfterOpenCloseOpenAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, ct: ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            // Close and re-open the client under test.
            await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            OutgoingMessage testMessage = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = testMessage;
            await serviceClient.Messages.SendAsync(testDevice.Id, testMessage, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={testMessage.MessageId} - to be received on callback");

            // The message should be received on the callback even though the client was re-opened.
            await deviceHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, ct: ct).ConfigureAwait(false);
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

            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);

            // This will make the client unsubscribe from the mqtt c2d topic/close the amqp c2d link. Neither event
            // should close the connection as a whole, though.
            await deviceClient.SetIncomingMessageCallbackAsync(null, ct).ConfigureAwait(false);

            await Task.Delay(1000, ct).ConfigureAwait(false);

            lostConnection.Should().BeFalse();
        }
    }
}
