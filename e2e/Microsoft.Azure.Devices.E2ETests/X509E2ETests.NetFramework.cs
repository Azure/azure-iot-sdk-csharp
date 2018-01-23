using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class X509E2ETests
    {
        private async Task SendSingleMessageX509(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDeviceWithX509(DevicePrefix, hostName, registryManager);

            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1);

            X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

            var auth = new DeviceAuthenticationWithX509Certificate(deviceInfo.Item1, cert);
            var deviceClient = DeviceClient.Create(deviceInfo.Item2, auth, transport);

            try
            {
                await deviceClient.OpenAsync();

                string payload;
                string p1Value;
                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage);

                bool isReceived = false;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!isReceived && sw.Elapsed.Minutes < 1)
                {
                    var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));
                    isReceived = VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);
                }
                sw.Stop();

                Assert.IsTrue(isReceived, "Message is not received.");
            }
            finally
            {
                await deviceClient.CloseAsync();
                await eventHubReceiver.CloseAsync();
                await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
            }
        }
        private async Task<EventHubReceiver> CreateEventHubReceiver(string deviceName)
        {
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(hubConnectionString);
            var eventHubPartitions = await eventHubClient.GetRuntimeInformationAsync();
            var eventHubPartitionsCount = eventHubPartitions.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.ConsumerGroup;
            return eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now, TestUtil.EventHubEpoch++);
        }

        private bool VerifyTestMessage(IEnumerable<EventData> events, string deviceName, string payload, string p1Value)
        {
            foreach (var eventData in events)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                if (data.Equals(payload))
                {
                    var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                    if (string.Equals(connectionDeviceId, deviceName, StringComparison.CurrentCultureIgnoreCase) &&
                        eventData.Properties.Count == 1 &&
                        eventData.Properties.Single().Key.Equals("property1") &&
                        eventData.Properties.Single().Value.Equals(p1Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
