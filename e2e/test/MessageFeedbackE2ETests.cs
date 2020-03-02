using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    // TODO MQTT OrderedTwoPhaseWorkQueue disallow message feedback to be called mix order, enable this test once it's fixed
    public class MessageFeedbackE2ETests
    {
        private const int MESSAGE_COUNT = 5;

        private static readonly string DevicePrefix = $"E2E_{nameof(MessageFeedbackE2ETests)}_";
        private static readonly TestLogging _log = TestLogging.GetInstance();
        private static readonly TimeSpan TIMESPAN_ONE_MINUTE = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan TIMESPAN_FIVE_SECONDS = TimeSpan.FromSeconds(5);

        private readonly ConsoleEventListener _listener;

        public MessageFeedbackE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_CompleteMixOrder_AMQP()
        {
            await CompleteMessageMixOrder(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        private static async Task CompleteMessageMixOrder(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                var messages = new List<Client.Message>();
                for (int i = 0; i < MESSAGE_COUNT; i++)
                {
                    (Message msg, string messageId, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2DTestMessage();
                    await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                    Client.Message message = await deviceClient.ReceiveAsync(TIMESPAN_ONE_MINUTE).ConfigureAwait(false);
                    if (message == null)
                    {
                        Assert.Fail("No message received.");
                    }
                    messages.Add(message);
                }

                for (int i = 0; i < MESSAGE_COUNT; i++)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await deviceClient.CompleteAsync(messages[MESSAGE_COUNT - 1 - i]).ConfigureAwait(false);
                    stopwatch.Stop();
                    Assert.IsFalse(stopwatch.ElapsedMilliseconds > deviceClient.OperationTimeoutInMilliseconds, $"CompleteAsync is over {deviceClient.OperationTimeoutInMilliseconds}");
                }

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
