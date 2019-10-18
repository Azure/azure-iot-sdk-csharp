// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public delegate Task TestDelegate(DeviceClient dc, Message msg);

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
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            //await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
            //await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
            await ReceiveMsultipleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            //await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
            //await ReceiveMsultipleMessage(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
            await SendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
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
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage = null;

                _log.WriteLine($"Receiving messages for device {deviceId}.");
                if (transport == Client.TransportType.Http1)
                {
                    // timeout on HTTP is not supported
                    receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    _log.WriteLine($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");

                    Assert.AreEqual(payload, messageData, $"The payload did not match for device {deviceId}");

                    Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                    Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
                    
                    try
                    {
                        await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        /*
                            Catch exception and break out of loop as this is expected.
                            If no exception happens it will throw an exception
                         */
                        break;
                    }
                    break;
                    //throw new Exception("Unexpected behavior acknowledging C2D messages in different order");

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

                _log.WriteLine($"Receiving messages for device {deviceId}.");
                if (transport == Client.TransportType.Http1)
                {
                    // timeout on HTTP is not supported
                    receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    _log.WriteLine($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");

                    Assert.AreEqual(payload, messageData, $"The payload did not match for device {deviceId}");

                    Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                    Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");

                    await dc.RejectAsync(receivedMessage).ConfigureAwait(false);

                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    Assert.IsNull(receivedMessage);
                    break;
                }

                if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        public static async Task VerifyReceivedC2DMessageAndAbandon(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value, TestDelegate messageTest)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                _log.WriteLine($"Receiving messages for device {deviceId}.");
                if (transport == Client.TransportType.Http1)
                {
                    // timeout on HTTP is not supported
                    receivedMessage = await dc.ReceiveAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                if (receivedMessage != null)
                {
                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);

                    //await dc.AbandonAsync(receivedMessage).ConfigureAwait(false);
                    await messageTest(dc, receivedMessage).ConfigureAwait(false);

                    receivedMessage = await dc.ReceiveAsync(cts.Token).ConfigureAwait(false);

                    Assert.IsNotNull(receivedMessage);

                    VerifyMessage(deviceId, payload, p1Value, receivedMessage);

                    break;
                }

                if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        private static void VerifyMessage(string deviceId, string payload, string p1Value, Client.Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            _log.WriteLine($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");

            Assert.AreEqual(payload, messageData, $"The payload did not match for device {deviceId}");

            Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
            var prop = receivedMessage.Properties.Single();
            Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
            Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
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
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                Task test(DeviceClient dc, Client.Message message) => dc.AbandonAsync(message);
                //await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);
                //await VerifyReceivedC2DMessageAndReject(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAndAbandon(transport, deviceClient, testDevice.Id, payload, p1Value, test).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveMsultipleMessage(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            Client.Message receivedMessage1, receivedMessage2, receivedMessage3 = null;
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);
                (Message msg1, string messageId1, string payload1, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg1).ConfigureAwait(false);
                (Message msg2, string messageId2, string payload2, string p2Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg2).ConfigureAwait(false);
                (Message msg3, string messageId3, string payload3, string p3Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg3).ConfigureAwait(false);

                receivedMessage1 = await deviceClient.ReceiveAsync().ConfigureAwait(false);
                receivedMessage2 = await deviceClient.ReceiveAsync().ConfigureAwait(false);
                //receivedMessage3 = await deviceClient.ReceiveAsync().ConfigureAwait(false);

                await deviceClient.CompleteAsync(receivedMessage2).ConfigureAwait(false);
                await deviceClient.AbandonAsync(receivedMessage1).ConfigureAwait(false);
                //await deviceClient.RejectAsync(receivedMessage3).ConfigureAwait(false);

                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload1, p1Value).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload3, p3Value).ConfigureAwait(false);
                //await VerifyReceivedC2DMessageAndReject(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);
                //await VerifyReceivedC2DMessageAndAbandon(transport, deviceClient, testDevice.Id, payload2, p2Value).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task SendReceiveMultipleMessages(TestDeviceType type, Client.TransportType transport)
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
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);
                for (int i = 0; i < 50; i++)
                {
                    (Message msg1, string messageId1, string payload1, string p1Value) = ComposeC2DTestMessage();
                    await serviceClient.SendAsync(testDevice.Id, msg1).ConfigureAwait(false);
                }
                try
                {
                    (Message msg2, string messageId2, string payload2, string p2Value) = ComposeC2DTestMessage();
                    await serviceClient.SendAsync(testDevice.Id, msg2).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is DeviceMaximumQueueDepthExceededException)
                    {
                        // This is expected. 
                        // ToDo: Find how this exception can be caught
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
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
