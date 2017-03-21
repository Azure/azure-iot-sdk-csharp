using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System.Linq;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class MessageE2ETests
    {
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        private readonly SemaphoreSlim sequentialTestSemaphore = new SemaphoreSlim(1, 1);

        public TestContext TestContext { get; set; }
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment("E2E_Message_CSharp_");
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
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_Tcp_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossSendRecovery_Http()
        {
            await SendMessageRecovery(Client.TransportType.Http1, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpConnection", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpConnection", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpSession", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpSession", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpD2CLink", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpD2CLink", "boom", 1);
        }


        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_Tcp_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Http()
        {
            await ReceiveMessageRecovery(Client.TransportType.Http1, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpConnection", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpConnection", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpSession", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpSessionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpSession", "", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpC2DLink", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        [TestCategory("Recovery")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpC2DLink", "boom", 1);
        }
   
        private EventHubReceiver CreateEventHubReceiver(string deviceName, out EventHubClient eventHubClient)
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(hubConnectionString, "messages/events");
            var eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Environment.GetEnvironmentVariable("IOTHUB_EVENTHUB_CONSUMER_GROUP") ?? "$Default";
            return eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now);
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

        private void VerifyTestMessage(IEnumerable<EventData> events, string deviceName, string payload, string p1Value)
        {
            foreach (var eventData in events)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                Assert.AreEqual(data, payload);

                var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                Assert.AreEqual(connectionDeviceId.ToUpper(), deviceName.ToUpper());

                Assert.AreEqual(eventData.Properties.Count, 1);
                var property = eventData.Properties.Single();
                Assert.AreEqual(property.Key, "property1");
                Assert.AreEqual(property.Value, p1Value);
            }
        }

        private async Task SendSingleMessage(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);

            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = CreateEventHubReceiver(deviceInfo.Item1, out eventHubClient);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();

            string payload;
            string p1Value;
            Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
            await deviceClient.SendEventAsync(testMessage);

            var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));
            VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);

            await deviceClient.CloseAsync();
            await eventHubClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
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
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();
            if (transport == Client.TransportType.Mqtt_Tcp_Only || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2));
            }

            string payload, messageId, p1Value;
            await serviceClient.OpenAsync();
            await serviceClient.SendAsync(deviceInfo.Item1, ComposeC2DTestMessage(out payload, out messageId, out p1Value));

            await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);

            await deviceClient.CloseAsync();
            await serviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task SendMessageRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);

            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = CreateEventHubReceiver(deviceInfo.Item1, out eventHubClient);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();

            string payload, p1Value;
            Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
            await deviceClient.SendEventAsync(testMessage);

            var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));
            VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);

            // send error command and clear eventHubReceiver of the fault injection message
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));
            await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));

            Thread.Sleep(1000);
            testMessage = ComposeD2CTestMessage(out payload, out p1Value);
            await deviceClient.SendEventAsync(testMessage);

            events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));
            VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);

            await deviceClient.CloseAsync();
            await eventHubClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);

            sequentialTestSemaphore.Release(1);
        }

        private async Task ReceiveMessageRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();
            if (transport == Client.TransportType.Mqtt_Tcp_Only || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2));
            }

            string payload, messageId, p1Value;
            await serviceClient.OpenAsync();
            await serviceClient.SendAsync(deviceInfo.Item1, ComposeC2DTestMessage(out payload, out messageId, out p1Value));
            await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

            Thread.Sleep(1000);
            await serviceClient.SendAsync(deviceInfo.Item1, ComposeC2DTestMessage(out payload, out messageId, out p1Value));
            await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value);

            await deviceClient.CloseAsync();
            await serviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);

            sequentialTestSemaphore.Release(1);
        }
    }
}

