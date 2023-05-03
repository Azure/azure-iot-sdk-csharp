﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for MessageFeedbackReceiver.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    [TestCategory("Serial")]
    public class MessageFeedbackReceiverE2ETest : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(MessageFeedbackReceiverE2ETest)}_";

        [TestMethod]
        [DoNotParallelize]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessageFeedbackReceiver_Operation(IotHubTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol,
            };
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);

            try
            {
                // Configure the device to receive messages.
                await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));
                await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

                var c2dMessageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<MessageAcknowledgement> OnC2DMessageReceived(IncomingMessage message)
                {
                    c2dMessageReceived.TrySetResult(true);
                    return Task.FromResult(MessageAcknowledgement.Complete);
                }
                await deviceClient.SetIncomingMessageCallbackAsync(OnC2DMessageReceived).ConfigureAwait(false);

                // Configure the service client to send the message.
                var message = new OutgoingMessage("some payload")
                {
                    Ack = DeliveryAcknowledgement.Full,
                    MessageId = Guid.NewGuid().ToString(),
                };
                var feedbackMessageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                serviceClient.MessageFeedback.MessageFeedbackProcessor = (FeedbackBatch feedback) =>
                {
                    if (feedback.Records.Any(x => x.OriginalMessageId == message.MessageId))
                    {
                        feedbackMessageReceived.TrySetResult(true);
                        return Task.FromResult(AcknowledgementType.Complete);
                    }

                    // Same hub as other tests, so we don't want to complete messages that aren't meant for us.
                    return Task.FromResult(AcknowledgementType.Abandon);
                };
                await serviceClient.MessageFeedback.OpenAsync().ConfigureAwait(false);

                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                await serviceClient.Messages.SendAsync(testDevice.Device.Id, message).ConfigureAwait(false);

                // Wait for the device to receive the message.
                await c2dMessageReceived.WaitAsync(cts.Token).ConfigureAwait(false);

                // Wait for the service to receive the feedback message.
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(200));
                await feedbackMessageReceived.WaitAsync(cts2.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                VerboseTestLogger.WriteLine($"Test {nameof(MessageFeedbackReceiver_Operation)} failed over {protocol} due to {ex}");
                throw;
            }
            finally
            {
                try
                {
                    await serviceClient.MessageFeedback.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"Cleanup in {nameof(MessageFeedbackReceiver_Operation)} failed over {protocol} due to {ex}");
                }
            }
        }

        [TestMethod]
        public async Task MessageFeedbackReceiver_CloseGracefully_DoesNotExecuteConnectionLoss()
        {
            // arrange
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            bool connectionLossEventExecuted = false;
            Func<MessageFeedbackProcessorError, Task> OnConnectionLostAsync = delegate
            {
                // There is a small chance that this test's connection is interrupted by an actual
                // network failure (when this callback should be executed), but the operations
                // tested here are so quick that it should be safe to ignore that possibility.
                connectionLossEventExecuted = true;
                return Task.CompletedTask;
            };
            sender.MessageFeedback.ErrorProcessor = OnConnectionLostAsync;

            Task<AcknowledgementType> OnFeedbackMessageReceivedAsync(FeedbackBatch feedbackBatch)
            {
                // No feedback messages belong to this test, so abandon any that it may receive
                return Task.FromResult(AcknowledgementType.Abandon);
            }
            sender.MessageFeedback.MessageFeedbackProcessor = OnFeedbackMessageReceivedAsync;

            await sender.MessageFeedback.OpenAsync().ConfigureAwait(false);
            
            // act
            await sender.MessageFeedback.CloseAsync().ConfigureAwait(false);

            // assert
            Assert.IsFalse(
                connectionLossEventExecuted,
                "One or more connection lost events were reported by the error processor unexpectedly");
        }
    }
}
