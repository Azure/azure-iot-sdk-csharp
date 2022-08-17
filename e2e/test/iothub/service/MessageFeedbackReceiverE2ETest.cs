// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for MessageFeedbackReceiver.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessageFeedbackReceiverE2eTest : E2EMsTestBase
    {
        private bool messagedFeedbackReceived;
        [TestMethod]
        public async Task MessageFeedbackReceiver_Operation()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            serviceClient.MessageFeedbackProcessor.MessageFeedbackProcessor = feedbackCallback;
            await serviceClient.MessageFeedbackProcessor.OpenAsync().ConfigureAwait(false);
            var message = new Message(Encoding.UTF8.GetBytes("some payload"));
            message.Ack = DeliveryAcknowledgement.Full;
            messagedFeedbackReceived = false;
            await serviceClient.Messaging.SendAsync(TestConfiguration.IoTHub.X509ChainDeviceName, message).ConfigureAwait(false);
            await receiveMessage().ConfigureAwait(false);

            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 60000)
            {
                continue;
            }
            await serviceClient.MessageFeedbackProcessor.CloseAsync();
            messagedFeedbackReceived.Should().BeTrue();
        }

        private AcknowledgementType feedbackCallback(FeedbackBatch feedback)
        {
            messagedFeedbackReceived = true;
            return AcknowledgementType.Complete;
        }
        private async Task receiveMessage()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString($"{TestConfiguration.IoTHub.ConnectionString};DeviceId={TestConfiguration.IoTHub.X509ChainDeviceName}");
            Client.Message message = await deviceClient.ReceiveMessageAsync().ConfigureAwait(false);
            await deviceClient.CompleteMessageAsync(message.LockToken).ConfigureAwait(false);
        }
    }
}
