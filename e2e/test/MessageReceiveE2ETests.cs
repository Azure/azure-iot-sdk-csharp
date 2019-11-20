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
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
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

        private async Task ReceiveMessageInOperationTimeout(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                try
                {
                    deviceClient.OperationTimeoutInMilliseconds = Convert.ToUInt32(TIMESPAN_ONE_MINUTE.TotalMilliseconds);
                    if (transport == Client.TransportType.Amqp || transport == Client.TransportType.Amqp_Tcp_Only || transport == Client.TransportType.Amqp_WebSocket_Only)
                    {
                        await ReceiveMessageWithoutTimeoutCheck(deviceClient, TIMESPAN_ONE_MINUTE).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReceiveMessageWithoutTimeoutCheck(deviceClient, TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                    }
                }
                finally
                {
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
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
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
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task ReceiveMessageWithoutTimeoutCheck(DeviceClient dc, TimeSpan bufferTime)
        {
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                try
                {
                    sw.Start();
                    Client.Message message = await dc.ReceiveAsync().ConfigureAwait(false);
                    if (message == null)
                    {
                        break;
                    }
                }
                finally
                {
                    sw.Stop();
                    if (sw.Elapsed > (TimeSpan.FromMilliseconds(dc.OperationTimeoutInMilliseconds) + bufferTime))
                    {
                        Assert.Fail("ReceiveAsync did not return in Operation Timeout time.");
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
                    Client.Message message = await dc.ReceiveAsync().ConfigureAwait(false);
                    if (message == null)
                    {
                        break;
                    }
                }
                finally
                {
                    sw.Stop();
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
