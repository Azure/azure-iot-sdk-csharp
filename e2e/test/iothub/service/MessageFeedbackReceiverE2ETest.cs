using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public class MessageFeedbackReceiverE2ETest : E2EMsTestBase
    {
        private bool messagedFeedbackReceived;
        [TestMethod]
        public async Task MessageFeedbackReceiver_Operation()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            serviceClient.MessageFeedbackProcessor._messageFeedbackProcessor = feedbackCallback;
            await serviceClient.MessageFeedbackProcessor.OpenAsync().ConfigureAwait(false);
            using var message = new Message(Encoding.UTF8.GetBytes("some payload"));
            message.Ack = DeliveryAcknowledgement.Full;
            messagedFeedbackReceived = false;
            await serviceClient.Messaging.SendAsync("TestInstance", message).ConfigureAwait(false);
            await receiveMessage().ConfigureAwait(false);

            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 60000)
            {
                continue;
            }
            await serviceClient.MessageFeedbackProcessor.CloseAsync();
            Assert.IsTrue(messagedFeedbackReceived);
        }

        private DeliveryAcknowledgement feedbackCallback(FeedbackBatch feedback)
        {
            messagedFeedbackReceived = true;
            return DeliveryAcknowledgement.PositiveOnly;
        }
        private async Task receiveMessage()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString($"{TestConfiguration.IoTHub.ConnectionString};DeviceId={TestConfiguration.IoTHub.X509ChainDeviceName}");
            Client.Message message = await deviceClient.ReceiveMessageAsync().ConfigureAwait(false);
            await deviceClient.CompleteMessageAsync(message.LockToken).ConfigureAwait(false);
        }
    }
}
