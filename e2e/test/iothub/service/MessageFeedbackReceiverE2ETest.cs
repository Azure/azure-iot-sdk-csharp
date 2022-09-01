// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
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
        private bool messagedFeedbackReceived;

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
            serviceClient.MessageFeedbackProcessor.MessageFeedbackProcessor = OnFeedbackReceived;
            await serviceClient.MessageFeedbackProcessor.OpenAsync().ConfigureAwait(false);
            var message = new Message(Encoding.UTF8.GetBytes("some payload"));
            message.Ack = DeliveryAcknowledgement.Full;
            messagedFeedbackReceived = false;
            await serviceClient.Messaging.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messaging.SendAsync(testDevice.Device.Id, message).ConfigureAwait(false);

            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Client.Message receivedMessage = await deviceClient.ReceiveMessageAsync().ConfigureAwait(false);
            await deviceClient.CompleteMessageAsync(receivedMessage.LockToken).ConfigureAwait(false);

            var timer = Stopwatch.StartNew();
            while (!messagedFeedbackReceived && timer.ElapsedMilliseconds < 60000)
            {
                await Task.Delay(200);
            }
            timer.Stop();

            messagedFeedbackReceived.Should().BeTrue("Timed out waiting to receive message feedback.");

            await serviceClient.MessageFeedbackProcessor.CloseAsync().ConfigureAwait(false);
            messagedFeedbackReceived.Should().BeTrue();
        }

        private AcknowledgementType OnFeedbackReceived(FeedbackBatch feedback)
        {
            messagedFeedbackReceived = true;
            return AcknowledgementType.Complete;
        }
    }
}
