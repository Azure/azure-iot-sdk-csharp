using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System.Linq;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class MessageE2ETests
    {
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        public TestContext TestContext { get; set; }
        
        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment("E2E_Message_CSharp_");
            hubConnectionString = environment.Item1;
            registryManager = environment.Item2;
            hostName = TestUtil.GetHostName(hubConnectionString);
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            TestUtil.UnInitializeEnvironment(registryManager);
        }
        
        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Amqp()
        {
            await sendSingleMessage(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs()
        {
            await sendSingleMessage(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Mqtt()
        {
            await sendSingleMessage(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_MqttWs()
        {
            await sendSingleMessage(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceSendSingleMessage_Http()
        {
            await sendSingleMessage(Client.TransportType.Http1);
        }

        async Task sendSingleMessage(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(hubConnectionString, "messages/events");
            var eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceInfo.Item1, eventHubPartitionsCount);
            string consumerGroupName = Environment.GetEnvironmentVariable("IOTHUB_EVENTHUB_CONSUMER_GROUP");
            if (consumerGroupName == null)
            {
                consumerGroupName = "$Default";
            }
            EventHubReceiver eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();

            string dataBuffer = Guid.NewGuid().ToString();
            string propertyName = "property1";
            string propertyValue = Guid.NewGuid().ToString();
            Client.Message eventMessage = new Client.Message(Encoding.UTF8.GetBytes(dataBuffer));
            eventMessage.Properties[propertyName] = propertyValue;
            await deviceClient.SendEventAsync(eventMessage);

            var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));

            foreach (var eventData in events)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                Assert.AreEqual(data, dataBuffer);

                var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                Assert.AreEqual(connectionDeviceId.ToUpper(), deviceInfo.Item1.ToUpper());

                Assert.AreEqual(eventData.Properties.Count, 1);
                var property = eventData.Properties.Single();
                Assert.AreEqual(property.Key, propertyName);
                Assert.AreEqual(property.Value, propertyValue);
            }

            await deviceClient.CloseAsync();
            await eventHubClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await receiveSingleMessage(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await receiveSingleMessage(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await receiveSingleMessage(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await receiveSingleMessage(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Message-E2E")]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await receiveSingleMessage(Client.TransportType.Http1);
        }

        async Task receiveSingleMessage(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Message_CSharp_", hostName, registryManager);
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            string dataBuffer = Guid.NewGuid().ToString();
            var serviceMessage = new Message(Encoding.ASCII.GetBytes(dataBuffer));
            serviceMessage.MessageId = Guid.NewGuid().ToString();

            string propertyName = "property1";
            string propertyValue = Guid.NewGuid().ToString();
            serviceMessage.Properties[propertyName] = propertyValue;

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();
            if (transport == Client.TransportType.Mqtt_Tcp_Only || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2));
            }

            await serviceClient.OpenAsync();
            await serviceClient.SendAsync(deviceInfo.Item1, serviceMessage);

            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage;

                if (transport == Client.TransportType.Http1)
                {
                    // Long-polling is not supported in http
                    receivedMessage = await deviceClient.ReceiveAsync();
                }
                else
                {
                    receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1));
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Assert.AreEqual(messageData, dataBuffer);

                    Assert.AreEqual(receivedMessage.Properties.Count, 1);
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual(prop.Key, propertyName);
                    Assert.AreEqual(prop.Value, propertyValue);

                    await deviceClient.CompleteAsync(receivedMessage);
                    wait = false;
                }

                if (sw.Elapsed.TotalSeconds > 5)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }
            sw.Stop();

            await deviceClient.CloseAsync();
            await serviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}

