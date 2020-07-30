// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("LongRunning")]
    public partial class MessageReceiveE2ETests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"E2E_{nameof(MessageReceiveE2ETests)}_";

        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_twentySeconds = TimeSpan.FromSeconds(20);

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_Amqp()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, s_oneSecond).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_Amqp()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, s_twentySeconds).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_AmqpWs()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, s_oneSecond).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_AmqpWs()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only, s_twentySeconds).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_Mqtt()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, s_oneSecond).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_Mqtt()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only, s_twentySeconds).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithLessTimeout_MqttWs()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, s_oneSecond).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageWithMoreTimeout_MqttWs()
        {
            await ReceiveMessageWithTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only, s_twentySeconds).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Amqp()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_AmqpWs()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Mqtt()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceReceiveMessageOperationTimeout_MqttWs()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        public static (Message message, string payload, string p1Value) ComposeC2dTestMessage(TestLogger logger)
        {
            var payload = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeC2dTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, payload, p1Value);
        }

        public static async Task VerifyReceivedC2DMessageAsync(Client.TransportType transport, DeviceClient dc, string deviceId, Message message, string payload, TestLogger logger)
        {
            string receivedMessageDestination = $"/devices/{deviceId}/messages/deviceBound";

            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received && sw.ElapsedMilliseconds < FaultInjection.RecoveryTimeMilliseconds)
            {
                Client.Message receivedMessage = null;

                try
                {
                    logger.Trace($"Receiving messages for device {deviceId}.");

                    if (transport == Client.TransportType.Http1)
                    {
                        // timeout on HTTP is not supported
                        receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        receivedMessage = await dc.ReceiveAsync(s_oneMinute).ConfigureAwait(false);
                    }

                    if (receivedMessage == null)
                    {
                        Assert.Fail($"No message is received for device {deviceId} in {s_oneMinute}.");
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

                    Assert.AreEqual(receivedMessage.MessageId, message.MessageId, "Recieved message Id is not what was sent by service");
                    Assert.AreEqual(receivedMessage.UserId, message.UserId, "Recieved user Id is not what was sent by service");
                    Assert.AreEqual(receivedMessage.To, receivedMessageDestination, "Recieved message destination is not what was sent by service");

                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    logger.Trace($"{nameof(VerifyReceivedC2DMessageAsync)}: Received message: for {deviceId}: {messageData}");
                    if (Equals(payload, messageData))
                    {
                        Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                        System.Collections.Generic.KeyValuePair<string, string> prop = receivedMessage.Properties.Single();
                        string propertyKey = "property1";
                        Assert.AreEqual(propertyKey, prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                        Assert.AreEqual(message.Properties[propertyKey], prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
                        received = true;
                    }
                }
                finally
                {
                    receivedMessage?.Dispose();
                }
            }

            sw.Stop();
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTimeMilliseconds}.");
        }

        public static async Task VerifyReceivedC2dMessageWithCancellationTokenAsync(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value, TestLogger logger)
        {
            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received && sw.ElapsedMilliseconds < FaultInjection.RecoveryTimeMilliseconds)
            {
                logger.Trace($"Receiving messages for device {deviceId}.");

                using var cts = new CancellationTokenSource(s_oneMinute);
                using Client.Message receivedMessage = await dc.ReceiveAsync(cts.Token).ConfigureAwait(false);

                if (receivedMessage == null)
                {
                    Assert.Fail($"No message is received for device {deviceId} in {s_oneMinute}.");
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
                logger.Trace($"{nameof(VerifyReceivedC2dMessageWithCancellationTokenAsync)}: Received message: for {deviceId}: {messageData}");
                if (Equals(payload, messageData))
                {
                    Assert.AreEqual(1, receivedMessage.Properties.Count, $"The count of received properties did not match for device {deviceId}");
                    System.Collections.Generic.KeyValuePair<string, string> prop = receivedMessage.Properties.Single();
                    Assert.AreEqual("property1", prop.Key, $"The key \"property1\" did not match for device {deviceId}");
                    Assert.AreEqual(p1Value, prop.Value, $"The value of \"property1\" did not match for device {deviceId}");
                    received = true;
                }
            }

            sw.Stop();
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTimeMilliseconds}.");
        }

        private async Task ReceiveMessageInOperationTimeoutAsync(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            Logger.Trace($"{nameof(ReceiveMessageInOperationTimeoutAsync)} - calling OpenAsync() for transport={transport}");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            try
            {
                deviceClient.OperationTimeoutInMilliseconds = Convert.ToUInt32(s_oneMinute.TotalMilliseconds);
                Logger.Trace($"{nameof(ReceiveMessageInOperationTimeoutAsync)} - setting device client default operation timeout={deviceClient.OperationTimeoutInMilliseconds} ms");

                if (transport == Client.TransportType.Amqp
                    || transport == Client.TransportType.Amqp_Tcp_Only
                    || transport == Client.TransportType.Amqp_WebSocket_Only)
                {
                    // TODO: this extra minute on the timeout is undesirable by customers, and tests seems to be failing on a slight timing issue.
                    // For now, add an additional 5 second buffer to prevent tests from failing, and meanwhile address issue 1203.

                    // For AMQP because of static 1 min interval check the cancellation token, in worst case it will block upto extra 1 min to return
                    await ReceiveMessageWithoutTimeoutCheckAsync(deviceClient, s_oneMinute + TimeSpan.FromSeconds(5), Logger).ConfigureAwait(false);
                }
                else
                {
                    await ReceiveMessageWithoutTimeoutCheckAsync(deviceClient, s_fiveSeconds, Logger).ConfigureAwait(false);
                }
            }
            finally
            {
                Logger.Trace($"{nameof(ReceiveMessageInOperationTimeoutAsync)} - calling CloseAsync() for transport={transport}");
                deviceClient.OperationTimeoutInMilliseconds = DeviceClient.DefaultOperationTimeoutInMilliseconds;
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveMessageWithTimeoutAsync(TestDeviceType type, Client.TransportType transport, TimeSpan timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            await ReceiveMessageWithTimeoutCheckAsync(deviceClient, timeout).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveSingleMessageAsync(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            await serviceClient.OpenAsync().ConfigureAwait(false);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, msg, payload, Logger).ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType type, Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            await serviceClient.OpenAsync().ConfigureAwait(false);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            }
            await VerifyReceivedC2dMessageWithCancellationTokenAsync(transport, deviceClient, testDevice.Id, payload, p1Value, Logger).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ReceiveMessageWithoutTimeoutCheckAsync(DeviceClient dc, TimeSpan bufferTime, TestLogger logger)
        {
            var sw = new Stopwatch();
            while (true)
            {
                try
                {
                    logger.Trace($"{nameof(ReceiveMessageWithoutTimeoutCheckAsync)} - Calling ReceiveAsync()");

                    sw.Restart();
                    using Client.Message message = await dc.ReceiveAsync().ConfigureAwait(false);
                    sw.Stop();

                    logger.Trace($"{nameof(ReceiveMessageWithoutTimeoutCheckAsync)} - Received message={message}; time taken={sw.ElapsedMilliseconds} ms");

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

        private static async Task ReceiveMessageWithTimeoutCheckAsync(DeviceClient dc, TimeSpan timeout)
        {
            while (true)
            {
                var sw = new Stopwatch();
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
                    if (sw.Elapsed > (timeout + s_fiveSeconds))
                    {
                        Assert.Fail("ReceiveAsync did not return in Operation Timeout time.");
                    }
                }
            }
        }
    }
}
