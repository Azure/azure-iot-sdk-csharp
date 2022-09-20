﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
    [TestCategory("IoTHub")]
    public class MessageFeedbackReceiverE2ETest : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(MessageFeedbackReceiverE2ETest)}_";

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessageFeedbackReceiver_Operation(IotHubTransportProtocol protocol)
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol,
            };
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);

            try
            {
                var message = new Message(Encoding.UTF8.GetBytes("some payload"))
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
                        return AcknowledgementType.Complete;
                    }

                    return AcknowledgementType.Abandon;
                };
                await serviceClient.MessageFeedback.OpenAsync().ConfigureAwait(false);

                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                await serviceClient.Messages.SendAsync(testDevice.Device.Id, message).ConfigureAwait(false);

                using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(
                    new IotHubClientOptions(new IotHubClientAmqpSettings()));
                await deviceClient.OpenAsync().ConfigureAwait(false);

                var c2dMessageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Func<Client.Message, object, Task<MessageResponse>> OnC2DMessageReceived = (message, context) =>
                {
                    c2dMessageReceived.SetResult(true);
                    return Task.FromResult(MessageResponse.Completed);
                };
                await deviceClient.SetReceiveMessageHandlerAsync(OnC2DMessageReceived, null).ConfigureAwait(false);

                Task result = await Task
                    .WhenAny(
                        // Wait for up to 200 seconds for the feedback message as the service may not send messages
                        // until they can batch others, even up to a minute later.
                        Task.Delay(TimeSpan.FromSeconds(200)),
                        c2dMessageReceived.Task)
                    .ConfigureAwait(false);
                feedbackMessageReceived.Task.IsCompleted.Should().BeTrue();
            }
            finally
            {
                await serviceClient.MessageFeedback.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
