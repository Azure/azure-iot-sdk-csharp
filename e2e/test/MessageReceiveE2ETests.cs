// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class MessageReceiveE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageReceiveE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            TimeSpan? timeout = TimeSpan.FromTicks(1);
            await TestMessageTimeout(timeout).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MessageReceive_DefaultTimeout_Ok()
        {
            TimeSpan? timeout = null;
            await TestMessageTimeout(timeout).ConfigureAwait(false);

        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        private async Task TestMessageTimeout(TimeSpan? timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Amqp))
            {
                await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), timeout).ConfigureAwait(false);
            }
        }

        private Client.Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: payload='{payload}' p1Value='{p1Value}'");

            return new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
        }

        private Message ComposeC2DTestMessage(out string payload, out string messageId, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            messageId = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeC2DTestMessage)}: payload='{payload}' messageId='{messageId}' p1Value='{p1Value}'");

            return new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };
        }

        private async Task VerifyReceivedC2DMessage(Client.TransportType transport, DeviceClient dc, string payload, string p1Value)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage;

                if (transport == Client.TransportType.Http1)
                {
                    // Long-polling is not supported in http
                    receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Assert.AreEqual(payload, messageData);

                    Assert.AreEqual(1, receivedMessage.Properties.Count);
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key);
                    Assert.AreEqual(p1Value, prop.Value);

                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                    wait = false;
                }

                if (sw.Elapsed.TotalSeconds > 5)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        private async Task ReceiveSingleMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    _log.WriteLine("Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.");
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }

                string payload, messageId, p1Value;
                await serviceClient.OpenAsync().ConfigureAwait(false);

                Message msg = ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
