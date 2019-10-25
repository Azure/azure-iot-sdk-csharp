// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public delegate Task ProcessMessage(Microsoft.Azure.Devices.Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1value);        

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class MessageReceiveE2ETests : IDisposable
    {
        private static readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageReceiveE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMultipleMessages_Amqp()
        {
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageComplete_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageAbandon_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, VerifyReceivedC2DMessageAndAbandon).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageReject_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, VerifyReceivedC2DMessageAndReject).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMultipleMessages_AmqpWs()
        {
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageComplete_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageAbandon_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, VerifyReceivedC2DMessageAndAbandon).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageReject_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, VerifyReceivedC2DMessageAndReject).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMultipleMessagess_Mqtt()
        {
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageComplete_Mqtt()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMultipleMessages_MqttWs()
        {
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageComplete_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageAbandon_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, VerifyReceivedC2DMessageAndAbandon).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageReject_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1, VerifyReceivedC2DMessageAndReject).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMultipleMessages_Http()
        {
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.X509, Client.TransportType.Http1, VerifyReceivedC2DMessageAndComplete).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceivesAbandonedMessge_Amqp()
        {
            await ReceiveAbandonedMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceivesAbandonedMessge_AmqpWs()
        {
            await ReceiveAbandonedMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceivesAbandonedMessge_Http()
        {
            await ReceiveAbandonedMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        public static (Message message, string messageId, string payload, string p1Value) ComposeC2DTestMessage()
        {
            var payload = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeC2DTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
        }
        private static async Task<Client.Message> ReceiveMessage(Client.TransportType transport, DeviceClient dc, string deviceId, CancellationTokenSource cts)
        {
            Client.Message receivedMessage = null;
            _log.WriteLine($"Receiving messages for device {deviceId}.");
            if (transport == Client.TransportType.Http1)
            {
                // timeout on HTTP is not supported
                receivedMessage = await dc.ReceiveAsync(cts.Token).ConfigureAwait(false);
            }
            else
            {
                receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            }

            return receivedMessage;
        }

        private static void VerifyMessage(string deviceId, string payload, string p1Value, Client.Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            _log.WriteLine($"{nameof(VerifyMessage)}: Received message: for {deviceId}: {messageData}");

            Assert.AreEqual(payload, messageData, $"The payload did not match for device {deviceId}");

            Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
            var prop = receivedMessage.Properties.Single();
            Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
            Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
        }

        public static async Task VerifyReceivedC2DMessageAndComplete(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);
                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    cts.Dispose();
                    cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);
                    Assert.IsNull(receivedMessage, $"received message is not NULL for device : {deviceId}");
                    break;
                }

                if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        public static async Task VerifyReceivedC2DMessageAndReject(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);
                    await dc.RejectAsync(receivedMessage).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);
                    Assert.IsNull(receivedMessage, $"received message is not NULL for device : {deviceId}");
                    break;
                }

                if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        public static async Task VerifyReceivedC2DMessageAndAbandon(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);
                    await dc.AbandonAsync(receivedMessage).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    receivedMessage = await ReceiveMessage(transport, dc, deviceId, cts).ConfigureAwait(false);
                    Assert.IsNotNull(receivedMessage, $"received message is NULL for device : {deviceId}");
                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);
                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);

                    break;
                }

                if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }


        private static async Task ReceiveSingleMessage(TestDeviceType type, Client.TransportType transport, ProcessMessage ReceiveAndAckMessage)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await serviceClient.OpenAsync().ConfigureAwait(false);
                PurgeMessageQueueResult purgeMessageQResult = await serviceClient.PurgeMessageQueueAsync(testDevice.Id).ConfigureAwait(false);
                _log.WriteLine($"Number of messages purged ='{purgeMessageQResult.TotalMessagesPurged}'");
                System.Threading.Thread.Sleep(2000);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await ReceiveAndAckMessage(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);
                System.Threading.Thread.Sleep(2000);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveAbandonedMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            
            Client.Message receivedMessage1, receivedMessage2;
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await serviceClient.OpenAsync().ConfigureAwait(false);
                PurgeMessageQueueResult purgeMessageQResult = await serviceClient.PurgeMessageQueueAsync(testDevice.Id).ConfigureAwait(false);
                _log.WriteLine($"Number of messages purged ='{purgeMessageQResult.TotalMessagesPurged}'");
                System.Threading.Thread.Sleep(2000);

                (Message msg1, string messageId1, string payload1, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg1).ConfigureAwait(false);
                (Message msg2, string messageId2, string payload2, string p2Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg2).ConfigureAwait(false);

                receivedMessage1 = await deviceClient.ReceiveAsync().ConfigureAwait(false);

                /* With AbandonAsync ordering of message is not guaranteed due to Prefetch Count set 
                 * on AMQP transport settings as there could be pending messages in local queue which 
                 * are received before receiving abandoned message so we are not testing when the message 
                 * is received but whether it is received */
                await deviceClient.AbandonAsync(receivedMessage1).ConfigureAwait(false);
                receivedMessage2 = await deviceClient.ReceiveAsync().ConfigureAwait(false);
                Client.Message receivedMessage3 = await deviceClient.ReceiveAsync().ConfigureAwait(false);

                /* There is msg2 still in the queue and msg1 due to AbandonAsync hence check for NULL */
                Assert.IsNotNull(receivedMessage2);
                Assert.IsNotNull(receivedMessage3);

                var prop2 = receivedMessage2.Properties.Single();
                var prop3 = receivedMessage3.Properties.Single();

                if (p1Value == prop2.Value)
                {
                    VerifyMessage(messageId1, payload1, p1Value, receivedMessage2);
                }
                else if (p1Value == prop3.Value)
                {
                    VerifyMessage(messageId1, payload1, p1Value, receivedMessage3);                    
                }
                else
                {
                    throw new InvalidOperationException(" Did not receive Abandoned message");
                }
                await deviceClient.CompleteAsync(receivedMessage2).ConfigureAwait(false);
                await deviceClient.CompleteAsync(receivedMessage3).ConfigureAwait(false);
                System.Threading.Thread.Sleep(2000);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task SendReceiveMultipleMessages(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    CancellationTokenSource cts20 = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    await deviceClient.ReceiveAsync(cts20.Token).ConfigureAwait(false);
                }
                
                
                await serviceClient.OpenAsync().ConfigureAwait(false);
                PurgeMessageQueueResult purgeMessageQResult = await serviceClient.PurgeMessageQueueAsync(testDevice.Id).ConfigureAwait(false);
                _log.WriteLine($"Number of messages purged ='{purgeMessageQResult.TotalMessagesPurged}'");
                System.Threading.Thread.Sleep(2000);

                int numMessages = 0;
                CancellationTokenSource cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                /* DeviceQueue on service holds at max 50 messages which are not acked i.e. complete/reject/abandon 
                 * from device Client, for receiving the 51st message without acking previous exception should be raised */
                try
                {
                    for (int i = 0; i < 51; i++)
                    {
                        (Message msg1, string messageId1, string payload1, string p1Value) = ComposeC2DTestMessage();
                        msg1.ExpiryTimeUtc = DateTime.Now.AddHours(1);
                        await serviceClient.SendAsync(testDevice.Id, msg1).ConfigureAwait(false);
                        numMessages++;
                    }
                    // If no exception is thrown then it gets here
                    Assert.Fail($"Number of C2D messages sent so far are : {numMessages} device : {testDevice.Id}"); 
                }
                catch (Exception ex)
                {
                    if (ex is DeviceMaximumQueueDepthExceededException)
                    {
                        // This is expected. 
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    purgeMessageQResult = await serviceClient.PurgeMessageQueueAsync(testDevice.Id).ConfigureAwait(false);
                    _log.WriteLine($"Number of messages purged ='{purgeMessageQResult.TotalMessagesPurged}'");
                    System.Threading.Thread.Sleep(2000);
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                    await serviceClient.CloseAsync().ConfigureAwait(false);
                }
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
