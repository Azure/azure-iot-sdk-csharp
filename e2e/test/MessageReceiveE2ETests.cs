// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        public async Task Message_DeviceReceive_Amqp_Validate_Timeout()
        {
            await ReceiveAsyncTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceive_AmqpWs_Validate_Timeout()
        {
            await ReceiveAsyncTimeout(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Message_DeviceReceive_Amqp_Validate_CancellationToken()
        {
            await ReceiveAsyncCancel(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Message_DeviceReceive_AmqpWs_Validate_CancellationToken()
        {
            await ReceiveAsyncCancel(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
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
        public async Task Message_DeviceReceive_Mqtt_Validate_Timeout()
        {
            await ReceiveAsyncTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Message_DeviceReceive_Mqtt_Validate_CancellationToken()
        {
            await ReceiveAsyncCancel(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Message_DeviceReceive_MqttWs_Validate_CancellationToken()
        {
            await ReceiveAsyncCancel(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceive_MqttWs_Validate_Timeout()
        {
            await ReceiveAsyncTimeout(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceive_Http_Validate_Timeout()
        {
            await ReceiveAsyncTimeout(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Message_DeviceReceive_Http_Validate_CancellationToken()
        {
            await ReceiveAsyncCancel(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
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
                receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

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

        private async Task ReceiveAsyncTimeout(TestDeviceType type, Client.TransportType transport)
        {
            var testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                const int timeout = 1_000;

                var sw = Stopwatch.StartNew();
                var receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(timeout)).ConfigureAwait(false);
                sw.Stop();

                Assert.IsNull(receivedMessage, "Found a message for test device without sending one");
                Assert.IsTrue(sw.ElapsedMilliseconds < 5_000, $"Expected timeout not met. Timeout fired after {sw.ElapsedMilliseconds}ms and requested was {timeout}ms");

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveAsyncCancel(TestDeviceType type, Client.TransportType transport)
        {
            var testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        const int timeout = 1_000;

                        var receiveTask = deviceClient.ReceiveAsync(cts.Token).ConfigureAwait(false);

                        var sw = Stopwatch.StartNew();
                        cts.Cancel();

                        try
                        {
                            await receiveTask;
                        }
                        catch (IotHubCommunicationException ex)
                        {
                            sw.Stop();
                            Assert.IsTrue(ex.InnerException is OperationCanceledException);
                            Assert.IsTrue(sw.ElapsedMilliseconds < 11_000, $"Expected timeout not met. Timeout fired after {sw.ElapsedMilliseconds}ms and requested was {timeout}ms");
                            throw ex.InnerException;
                        }
                    }
                }
                finally
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                }
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
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                (Message msg, string messageId, string payload, string p1Value) = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value).ConfigureAwait(false);

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
