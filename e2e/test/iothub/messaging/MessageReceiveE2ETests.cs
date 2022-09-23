// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
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
        private static readonly string s_devicePrefix = $"{nameof(MessageReceiveE2ETests)}_";

        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_tenSeconds = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_twentySeconds = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessageAsync(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageCancelled_Mqtt()
        {
            await Mqtt_ReceiveSingleMessageWithCancelledAsync(TestDeviceType.Sasl).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Amqp()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_AmqpWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Mqtt()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_MqttWs()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveSingleMessageWithCancellationToken_Http()
        {
            await ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType.X509, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Amqp()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageOperationTimeout_AmqpWs()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageOperationTimeout_Mqtt()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceReceiveMessageOperationTimeout_MqttWs()
        {
            await ReceiveMessageInOperationTimeoutAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task DeviceReceiveMessageUsingCallback_Http()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_Mqtt()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_MqttWs()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallback_Mqtt()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallback_MqttWs()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_MqttWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackAndUnsubscribe_Mqtt()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackAndUnsubscribe_MqttWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_Amqp()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallback_AmqpWs()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallback_Amqp()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallback_AmqpWs()
        {
            await ReceiveSingleMessageUsingCallbackAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackAndUnsubscribe_AmqpWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackAndUnsubscribe_Amqp()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackAndUnsubscribe_AmqpWs()
        {
            await ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Mqtt()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_MqttWs()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackUpdateHandler_Mqtt()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackUpdateHandler_MqttWs()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_Amqp()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceiveMessageUsingCallbackUpdateHandler_AmqpWs()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackUpdateHandler_Amqp()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceiveMessageUsingCallbackUpdateHandler_AmqpWs()
        {
            await ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_Mqtt()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_MqttWs()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_Amqp()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceReceivePendingMessageUsingCallback_AmqpWs()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceivePendingMessageUsingCallback_Mqtt()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceivePendingMessageUsingCallback_MqttWs()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceivePendingMessageUsingCallback_Amqp()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceReceivePendingMessageUsingCallback_AmqpWs()
        {
            await ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Mqtt()
        {
            var settings = new ITransportSettings[] { new MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only) { CleanSession = true } };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_MqttWs()
        {
            var settings = new ITransportSettings[] { new MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only) { CleanSession = true } };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_Amqp()
        {
            var settings = new ITransportSettings[] { new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only) };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceDoesNotReceivePendingMessageUsingCallback_AmqpWs()
        {
            var settings = new ITransportSettings[] { new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only) };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.Sasl, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceDoesNotReceivePendingMessageUsingCallback_Mqtt()
        {
            var settings = new ITransportSettings[] { new MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only) { CleanSession = true } };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceDoesNotReceivePendingMessageUsingCallback_MqttWs()
        {
            var settings = new ITransportSettings[] { new MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only) { CleanSession = true } };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceDoesNotReceivePendingMessageUsingCallback_Amqp()
        {
            var settings = new ITransportSettings[] { new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only) };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_DeviceDoesNotReceivePendingMessageUsingCallback_AmqpWs()
        {
            var settings = new ITransportSettings[] { new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only) };
            await DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType.X509, settings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Amqp()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_AmqpWs()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_Mqtt()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_DeviceMaintainsConnectionAfterUnsubscribing_MqttWs()
        {
            await UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        public static (Message message, string payload, string p1Value) ComposeC2dTestMessage(MsTestLogger logger)
        {
            string payload = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(ComposeC2dTestMessage)}: messageId='{messageId}' userId='{userId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, payload, p1Value);
        }

        public static async Task VerifyReceivedC2DMessageAsync(Client.TransportType transport, DeviceClient dc, string deviceId, Message message, string payload, MsTestLogger logger)
        {
            string receivedMessageDestination = $"/devices/{deviceId}/messages/deviceBound";

            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received && sw.Elapsed < FaultInjection.RecoveryTime)
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

                    Assert.AreEqual(receivedMessage.MessageId, message.MessageId, "Received message Id is not what was sent by service");
                    Assert.AreEqual(receivedMessage.UserId, message.UserId, "Received user Id is not what was sent by service");
                    Assert.AreEqual(receivedMessage.To, receivedMessageDestination, "Received message destination is not what was sent by service");

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
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTime}.");
        }

        public static async Task Mqtt_VerifyReceivedC2dMessageCancelledAsync(DeviceClient dc)
        {
            try
            {
                using var cts = new CancellationTokenSource(s_fiveSeconds);
                await dc.ReceiveAsync(cts.Token).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ex)
                when (ex.IsTransient && ex.InnerException is OperationCanceledException)
            {
                return;
            }

            Assert.Fail();
        }

        public static async Task VerifyReceivedC2dMessageWithCancellationTokenAsync(Client.TransportType transport, DeviceClient dc, string deviceId, string payload, string p1Value, MsTestLogger logger)
        {
            var sw = new Stopwatch();
            bool received = false;

            sw.Start();

            while (!received && sw.Elapsed < FaultInjection.RecoveryTime)
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
            Assert.IsTrue(received, $"No message received for device {deviceId} with payload={payload} in {FaultInjection.RecoveryTime}.");
        }

        private async Task ReceiveMessageInOperationTimeoutAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            Logger.Trace($"{nameof(ReceiveMessageInOperationTimeoutAsync)} - calling OpenAsync() for transport={transport}");
            await deviceClient.OpenAsync().ConfigureAwait(false);

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

        private async Task ReceiveSingleMessageAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await serviceClient.OpenAsync().ConfigureAwait(false);

            // For Mqtt - the device needs to have subscribed to the devicebound topic, in order for IoT hub to deliver messages to the device.
            // For this reason we will make a "fake" ReceiveAsync() call, which will result in the device subscribing to the c2d topic.
            // Note: We need this "fake" ReceiveAsync() call even though we (SDK default) CONNECT with a CleanSession flag set to 0.
            // This is because this test device is newly created, and it has never subscribed to IoT hub c2d topic.
            // Hence, IoT hub doesn't know about its CleanSession preference yet.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                await VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, msg, payload, Logger).ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Mqtt_ReceiveSingleMessageWithCancelledAsync(TestDeviceType type)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt_Tcp_Only);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // There is no message being sent so the device client should timeout waiting for the message.
            await Mqtt_VerifyReceivedC2dMessageCancelledAsync(deviceClient).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveSingleMessageWithCancellationTokenAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await serviceClient.OpenAsync().ConfigureAwait(false);

            // For Mqtt - the device needs to have subscribed to the devicebound topic, in order for IoT hub to deliver messages to the device.
            // For this reason we will make a "fake" ReceiveAsync() call, which will result in the device subscribing to the c2d topic.
            // Note: We need this "fake" ReceiveAsync() call even though we (SDK default) CONNECT with a CleanSession flag set to 0.
            // This is because this test device is newly created, and it has never subscribed to IoT hub c2d topic.
            // Hence, IoT hub doesn't know about its CleanSession preference yet.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
            }

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
            }
            await VerifyReceivedC2dMessageWithCancellationTokenAsync(transport, deviceClient, testDevice.Id, payload, p1Value, Logger).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ReceiveMessageWithoutTimeoutCheckAsync(DeviceClient dc, TimeSpan bufferTime, MsTestLogger logger)
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

        private async Task ReceiveSingleMessageUsingCallbackAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);
            using (msg)
            {
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
                testDeviceCallbackHandler.ExpectedMessageSentByService = msg;

                using var cts = new CancellationTokenSource(s_tenSeconds);
                Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
                await Task
                    .WhenAll(
                        serviceClient.SendAsync(testDevice.Id, msg),
                        testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token))
                    .ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackAndUnsubscribeAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            // For Mqtt - we will need to subscribe to the Mqtt receive telemetry topic
            // before the device can begin receiving c2d messages.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                Client.Message leftoverMessage = await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
                Logger.Trace($"Leftover message on Mqtt was: {leftoverMessage} with Id={leftoverMessage?.MessageId}");
            }

            // First receive message using the polling ReceiveAsync() API.
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, firstMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={firstMessage.MessageId} - to be received on polling ReceiveAsync");

            using Client.Message receivedFirstMessage = await deviceClient.ReceiveAsync(s_tenSeconds).ConfigureAwait(false);
            receivedFirstMessage.MessageId.Should().Be(firstMessage.MessageId);
            await deviceClient.CompleteAsync(receivedFirstMessage).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now, send a message to the device from the service.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            testDeviceCallbackHandler.ExpectedMessageSentByService = secondMessage;
            await serviceClient.SendAsync(testDevice.Id, secondMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={secondMessage.MessageId} - to be received on callback");

            // The message should be received on the callback, while a call to ReceiveAsync() should return null.
            using var cts = new CancellationTokenSource(s_tenSeconds);
            using Client.Message receivedSecondMessage = await deviceClient.ReceiveAsync(s_tenSeconds).ConfigureAwait(false);
            await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            receivedSecondMessage.Should().BeNull();

            // Now unsubscribe from receiving c2d messages over the callback.
            await deviceClient.SetReceiveMessageHandlerAsync(null, deviceClient).ConfigureAwait(false);

            // For Mqtt - since we have explicitly unsubscribed, we will need to resubscribe again
            // before the device can begin receiving c2d messages.
            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                Client.Message leftoverMessage = await deviceClient.ReceiveAsync(s_fiveSeconds).ConfigureAwait(false);
                Logger.Trace($"Leftover message on Mqtt was: {leftoverMessage} with Id={leftoverMessage?.MessageId}");
            }

            // Send a message to the device from the service.
            (Message thirdMessage, _, _) = ComposeC2dTestMessage(Logger);
            await serviceClient.SendAsync(testDevice.Id, thirdMessage).ConfigureAwait(false);
            Logger.Trace($"Sent C2D message from service, messageId={thirdMessage.MessageId} - to be received on polling ReceiveAsync");

            // This time, the message should not be received on the callback, rather it should be received on a call to ReceiveAsync().
            Func<Task> receiveMessageOverCallback = async () =>
            {
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            };
            using Client.Message receivedThirdMessage = await deviceClient.ReceiveAsync(s_tenSeconds).ConfigureAwait(false);
            receivedThirdMessage.MessageId.Should().Be(thirdMessage.MessageId);
            receiveMessageOverCallback.Should().Throw<OperationCanceledException>();
            await deviceClient.CompleteAsync(receivedThirdMessage).ConfigureAwait(false);

            firstMessage.Dispose();
            secondMessage.Dispose();
            thirdMessage.Dispose();

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessageUsingCallbackUpdateHandlerAsync(TestDeviceType type, Client.TransportType transport)
        {
            using var firstHandlerSemaphore = new SemaphoreSlim(0, 1);
            using var secondHandlerSemaphore = new SemaphoreSlim(0, 1);

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            // Set the first C2D message handler.
            await deviceClient.SetReceiveMessageHandlerAsync(
                async (message, context) =>
                {
                    Logger.Trace($"Received message over the first message handler: MessageId={message.MessageId}");
                    await deviceClient.CompleteAsync(message).ConfigureAwait(false);
                    firstHandlerSemaphore.Release();
                },
                deviceClient);

            // The C2D message should be received over the first callback handler, releasing the corresponding semaphore.
            using var firstCts = new CancellationTokenSource(s_tenSeconds);
            (Message firstMessage, _, _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={firstMessage.MessageId}");
            await Task
                .WhenAll(
                    serviceClient.SendAsync(testDevice.Id, firstMessage),
                    firstHandlerSemaphore.WaitAsync(firstCts.Token))
                .ConfigureAwait(false);

            // Set the second C2D message handler.
            await deviceClient.SetReceiveMessageHandlerAsync(
                async (message, context) =>
                {
                    Logger.Trace($"Received message over the second message handler: MessageId={message.MessageId}");
                    await deviceClient.CompleteAsync(message).ConfigureAwait(false);
                    secondHandlerSemaphore.Release();
                },
                deviceClient);

            using var secondCts = new CancellationTokenSource(s_tenSeconds);
            Func<Task> secondCallbackHandler = async () =>
            {
                await firstHandlerSemaphore.WaitAsync(secondCts.Token).ConfigureAwait(false);
            };

            // The C2D message should be received over the second callback handler, releasing the corresponding semaphore.
            // The first callback handler should not be called, meaning its semaphore should not be available to be grabbed.
            (Message secondMessage, _, _) = ComposeC2dTestMessage(Logger);
            Logger.Trace($"Sending C2D message from service, messageId={secondMessage.MessageId}");
            await Task
                    .WhenAll(
                        serviceClient.SendAsync(testDevice.Id, secondMessage),
                        secondHandlerSemaphore.WaitAsync(secondCts.Token))
                    .ConfigureAwait(false);
            secondCallbackHandler.Should().Throw<OperationCanceledException>();

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task ReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);
            var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now dispose and reinitialize the client instance.
            deviceClient.Dispose();
            deviceClient = null;

            testDeviceCallbackHandler.Dispose();
            testDeviceCallbackHandler = null;

            deviceClient = testDevice.CreateDeviceClient(transport);
            testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to false.
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Send the message from service.
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            // Subscribe to receive C2D messages over the callback.
            testDeviceCallbackHandler.ExpectedMessageSentByService = msg;
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Wait to ensure that the message was received.
            using var cts = new CancellationTokenSource(s_tenSeconds);
            await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);

            await serviceClient.CloseAsync().ConfigureAwait(false);
            deviceClient.Dispose();
            testDeviceCallbackHandler.Dispose();
        }

        private async Task DoNotReceiveMessagesSentBeforeSubscriptionAsync(TestDeviceType type, ITransportSettings[] settings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            DeviceClient deviceClient = testDevice.CreateDeviceClient(settings);
            var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            (Message msg, string payload, string p1Value) = ComposeC2dTestMessage(Logger);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Now dispose and reinitialize the client instance.
            deviceClient.Dispose();
            deviceClient = null;

            testDeviceCallbackHandler.Dispose();
            testDeviceCallbackHandler = null;

            deviceClient = testDevice.CreateDeviceClient(settings);
            testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            // Open the device client - for MQTT, this will connect the device with CleanSession flag set to true.
            // This will ensure that messages sent before the device had subscribed to c2d topic are not delivered.
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Send the message from service.
            Logger.Trace($"Sending C2D message from service, messageId={msg.MessageId}");
            await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

            // Subscribe to receive C2D messages over the callback.
            testDeviceCallbackHandler.ExpectedMessageSentByService = msg;
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // Wait to ensure that the message was not received.
            using var cts = new CancellationTokenSource(s_tenSeconds);
            Func<Task> receiveMessageOverCallback = async () =>
            {
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            };
            receiveMessageOverCallback.Should().Throw<OperationCanceledException>();

            await serviceClient.CloseAsync().ConfigureAwait(false);
            deviceClient.Dispose();
            testDeviceCallbackHandler.Dispose();
        }

        // This test ensures that the SDK does not have this bug again
        // https://github.com/Azure/azure-iot-sdk-csharp/issues/2218
        private async Task UnsubscribeDoesNotCauseConnectionStatusEventAsync(TestDeviceType type, Client.TransportType transportType)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType);
            bool lostConnection = false;
            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Disconnected_Retrying)
                {
                    lostConnection = true;
                }
            });

            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            // Subscribe to receive C2D messages over the callback.
            await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);

            // This will make the client unsubscribe from the mqtt c2d topic/close the amqp c2d link. Neither event
            // should close the connection as a whole, though.
            await deviceClient.SetReceiveMessageHandlerAsync(null, null).ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);

            lostConnection.Should().BeFalse();

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
