// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.EventHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public partial class MessageE2ETests
    {
        private const string DevicePrefix = "E2E_Message_CSharp_";
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        private readonly SemaphoreSlim sequentialTestSemaphore = new SemaphoreSlim(1, 1);

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment(DevicePrefix);
            hubConnectionString = environment.Item1;
            registryManager = environment.Item2;
            hostName = TestUtil.GetHostName(hubConnectionString);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestUtil.UnInitializeEnvironment(registryManager);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(Client.TransportType.Http1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessage(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessage(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(Client.TransportType.Http1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpConn,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpConn,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpSess,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpSess,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpD2C,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpD2C,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpConn,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [Ignore] // TODO: #239.
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpConn, "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpSess,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpSess,
                "",
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpC2D,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpD2C,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Throttle,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_ThrottledConnectionRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Throttle,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Throttle,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec,
                TestUtil.ShortRetryInMilliSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Throttle,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec,
                TestUtil.ShortRetryInMilliSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Http()
        {
            await SendMessageThrottledForHttp();
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.DeviceMaximumQueueDepthExceededException))]
        public async Task Message_QuotaExceededRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_QuotaExceeded,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.DeviceMaximumQueueDepthExceededException))]
        public async Task Message_QuotaExceededRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_QuotaExceeded,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.QuotaExceededException))]
        public async Task Message_QuotaExceededRecovery_Http()
        {
            await SendMessageRecovery(Client.TransportType.Http1,
                TestUtil.FaultType_QuotaExceeded,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Auth,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Auth,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_Http()
        {
            await SendMessageRecovery(Client.TransportType.Http1,
                TestUtil.FaultType_Auth,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec,
                TestUtil.DefaultDurationInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_SendGracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownSendRecovery_Mqtt()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_GracefulShutdownReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Timeout")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout();
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Timeout")]
        // Should not have any exceptions thrown. 
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout();
        }

        private async Task DefaultTimeout()
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            ServiceClient sender = ServiceClient.CreateFromConnectionString(hubConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, Client.TransportType.Amqp);
            await sender.SendAsync(deviceInfo.Item1, new Message(Encoding.ASCII.GetBytes("Dummy Message")), null);
        }

        private async Task FastTimeout()
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            ServiceClient sender = ServiceClient.CreateFromConnectionString(hubConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, Client.TransportType.Amqp);
            await sender.SendAsync(deviceInfo.Item1, new Message(Encoding.ASCII.GetBytes("Dummy Message")), TimeSpan.FromTicks(1));
        }

        private Client.Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

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
                    receivedMessage = await dc.ReceiveAsync();
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(1));
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Assert.AreEqual(messageData, payload);

                    Assert.AreEqual(receivedMessage.Properties.Count, 1);
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual(prop.Key, "property1");
                    Assert.AreEqual(prop.Value, p1Value);

                    await dc.CompleteAsync(receivedMessage);
                    wait = false;
                }

                if (sw.Elapsed.TotalSeconds > 5)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }
            sw.Stop();
        }

        private async Task ReceiveSingleMessage(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            try
            {

                await deviceClient.OpenAsync();
                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2));
                }

                string payload, messageId, p1Value;
                await serviceClient.OpenAsync();
                await serviceClient.SendAsync(deviceInfo.Item1,
                    ComposeC2DTestMessage(out payload, out messageId, out p1Value));

                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);
            }
            finally
            {
                await deviceClient.CloseAsync();
                await serviceClient.CloseAsync();
                await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
            }
        }
        
        private async Task ReceiveMessageRecovery(Client.TransportType transport, string faultType, string reason,
            int delayInSec)
        {
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            try
            {
                await deviceClient.OpenAsync();
                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2));
                }

                string payload, messageId, p1Value;
                await serviceClient.OpenAsync();
                await serviceClient.SendAsync(deviceInfo.Item1,
                    ComposeC2DTestMessage(out payload, out messageId, out p1Value));
                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);

                // send error command
                await deviceClient.SendEventAsync(
                    TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

                await Task.Delay(1000);
                await serviceClient.SendAsync(deviceInfo.Item1,
                    ComposeC2DTestMessage(out payload, out messageId, out p1Value));
                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);
            }
            finally
            {
                await deviceClient.CloseAsync();
                await serviceClient.CloseAsync();
                await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
                sequentialTestSemaphore.Release(1);
            }      
        }
    }
}

