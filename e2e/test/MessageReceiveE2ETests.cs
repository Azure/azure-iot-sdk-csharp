﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("LongRunning")]
    public partial class MessageReceiveE2ETests : IDisposable
    {
        private static readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveE2ETests)}_";
        private static readonly TestLogging _log = TestLogging.GetInstance();
        private static readonly TimeSpan TIMESPAN_ONE_MINUTE = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan TIMESPAN_ONE_SECOND = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan TIMESPAN_FIVE_SECONDS = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TIMESPAN_TWENDY_SECONDS = TimeSpan.FromSeconds(20);

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

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationToken(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_Amqp()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, TIMESPAN_ONE_SECOND).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_Amqp()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, TIMESPAN_TWENDY_SECONDS).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_AmqpWs()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, TIMESPAN_ONE_SECOND).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_AmqpWs()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, TIMESPAN_TWENDY_SECONDS).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_Mqtt()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, TIMESPAN_ONE_SECOND).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_Mqtt()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, TIMESPAN_TWENDY_SECONDS).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_MqttWs()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, TIMESPAN_ONE_SECOND).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_MqttWs()
        {
            await ReceiveMessageWithTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, TIMESPAN_TWENDY_SECONDS).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Amqp()
        {
            await ReceiveMessageInOperationTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_AmqpWs()
        {
            await ReceiveMessageInOperationTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Mqtt()
        {
            await ReceiveMessageInOperationTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_MqttWs()
        {
            await ReceiveMessageInOperationTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
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

        public static async Task VerifyReceivedC2DMessageAsync(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value)
        {
            Stopwatch sw = new Stopwatch();
            bool received = false;

            sw.Start();

            Client.Message receivedMessage;

            while (!received && sw.ElapsedMilliseconds < FaultInjection.RecoveryTimeMilliseconds)
            {
                _log.WriteLine($"Receiving messages for device {deviceId}.");

                if (transport == Client.TransportType.Http1)
                {
                    // timeout on HTTP is not supported
                    receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TIMESPAN_ONE_MINUTE).ConfigureAwait(false);
                }

                if (receivedMessage == null)
                {
                    Assert.Fail($"No message is received for device {deviceId} in {TIMESPAN_ONE_MINUTE}.");
                }

                try
                {
                    // always complete message
                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore exception from CompleteAsync
                }

                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                _log.WriteLine($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");
                if (Equals(payload, messageData))
                {
                    Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                    Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
                    received = true;
                }
            }

            sw.Stop();
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTimeMilliseconds}.");
        }

        public static async Task VerifyReceivedC2DMessageWithCancellationTokenAsync(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value)
        {
            Stopwatch sw = new Stopwatch();
            bool received = false;

            sw.Start();

            Client.Message receivedMessage;

            while (!received && sw.ElapsedMilliseconds < FaultInjection.RecoveryTimeMilliseconds)
            {
                _log.WriteLine($"Receiving messages for device {deviceId}.");

                receivedMessage = await dc.ReceiveAsync(new CancellationTokenSource(TIMESPAN_ONE_MINUTE).Token).ConfigureAwait(false);

                if (receivedMessage == null)
                {
                    Assert.Fail($"No message is received for device {deviceId} in {TIMESPAN_ONE_MINUTE}.");
                }

                try
                {
                    // always complete message
                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore exception from CompleteAsync
                }

                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                _log.WriteLine($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");
                if (Equals(payload, messageData))
                {
                    Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                    Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
                    received = true;
                }
            }

            sw.Stop();
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTimeMilliseconds}.");
        }

        private async Task ReceiveMessageInOperationTimeout(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                _log.WriteLine($"{nameof(ReceiveMessageInOperationTimeout)} - calling OpenAsync() for transport={transport}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                }

                try
                {
                    deviceClient.OperationTimeoutInMilliseconds = Convert.ToUInt32(TIMESPAN_ONE_MINUTE.TotalMilliseconds);
                    _log.WriteLine($"{nameof(ReceiveMessageInOperationTimeout)} - setting device client default operation timeout={deviceClient.OperationTimeoutInMilliseconds} ms");

                    if (transport == Client.TransportType.Amqp || transport == Client.TransportType.Amqp_Tcp_Only || transport == Client.TransportType.Amqp_WebSocket_Only)
                    {
                        // TODO: this extra minute on the timeout is undesirable by customers, and tests seems to be failing on a slight timing issue.
                        // For now, add an additional 5 second buffer to prevent tests from failing, and meanwhile address issue 1203.

                        // For AMQP because of static 1 min interval check the cancellation token, in worst case it will block upto extra 1 min to return
                        await ReceiveMessageWithoutTimeoutCheck(deviceClient, TIMESPAN_ONE_MINUTE + TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReceiveMessageWithoutTimeoutCheck(deviceClient, TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _log.WriteLine($"{nameof(ReceiveMessageInOperationTimeout)} - calling CloseAsync() for transport={transport}");
                    deviceClient.OperationTimeoutInMilliseconds = DeviceClient.DefaultOperationTimeoutInMilliseconds;
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task ReceiveMessageWithTimeout(TestDeviceType type, Client.TransportType transport, TimeSpan timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                }

                await ReceiveMessageWithTimeoutCheck(deviceClient, timeout).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
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
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveSingleMessageWithCancellationToken(TestDeviceType type, Client.TransportType transport)
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

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageWithCancellationTokenAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task ReceiveMessageWithoutTimeoutCheck(DeviceClient dc, TimeSpan bufferTime)
        {
            Stopwatch sw = new Stopwatch();
            while (true)
            {
                try
                {
                    _log.WriteLine($"{nameof(ReceiveMessageWithoutTimeoutCheck)} - Calling ReceiveAsync()");

                    sw.Restart();
                    Client.Message message = await dc.ReceiveAsync().ConfigureAwait(false);
                    sw.Stop();

                    _log.WriteLine($"{nameof(ReceiveMessageWithoutTimeoutCheck)} - Received message={message}; time taken={sw.ElapsedMilliseconds} ms");

                    if (message == null)
                    {
                        break;
                    }

                    await dc.CompleteAsync(message).ConfigureAwait(false);
                }
                finally
                {
                    TimeSpan maxLatency = TimeSpan.FromMilliseconds(dc.OperationTimeoutInMilliseconds) + bufferTime;
                    if (sw.Elapsed > maxLatency)
                    {
                        Assert.Fail($"ReceiveAsync did not return in {maxLatency}, instead it took {sw.Elapsed}.");
                    }
                }
            }
        }

        private static async Task ReceiveMessageWithTimeoutCheck(DeviceClient dc, TimeSpan timeout)
        {
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                try
                {
                    sw.Start();
                    Client.Message message = await dc.ReceiveAsync(timeout).ConfigureAwait(false);
                    sw.Stop();

                    if (message == null)
                    {
                        break;
                    }

                    await dc.CompleteAsync(message).ConfigureAwait(false);
                }
                finally
                {
                    if (sw.Elapsed > (timeout + TIMESPAN_FIVE_SECONDS))
                    {
                        Assert.Fail("ReceiveAsync did not return in Operation Timeout time.");
                    }
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
