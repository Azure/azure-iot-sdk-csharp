using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.EventHubs;
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
        //// This function create a device with x509 cert and send a message to the iothub on the transport specified.
        //// It then verifies the message is received at the eventHubClient.
        private async Task SendSingleMessageX509(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDeviceWithX509(DevicePrefix, hostName, registryManager);

            EventHubClient eventHubClient;
            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1);

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




        private async Task<PartitionReceiver> CreateEventHubReceiver(string deviceName)
        {
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString("EVENT HUB COMPATIBLE ENDPOINT");
            var eventHubRuntime = await eventHubClient.GetRuntimeInformationAsync();
            var eventHubPartitionsCount = eventHubRuntime.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.ConsumerGroup;
            return eventHubClient.CreateReceiver(consumerGroupName, partition, DateTime.Now.AddMinutes(-5));
        }
        private bool VerifyTestMessage(IEnumerable<EventData> events, string deviceName, string payload, string p1Value)
        {
            foreach (var eventData in events)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.ToArray());
                if (data.Equals(payload))
                {
                    var connectionDeviceId = eventData.Properties["iothub-connection-device-id"].ToString();
                    if (string.Equals(connectionDeviceId, deviceName, StringComparison.CurrentCultureIgnoreCase) &&
                        eventData.Properties.Count == 1 &&
                        VerifyKeyValue("property1", p1Value, eventData.Properties))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool VerifyKeyValue(string checkForKey, string checkForValue, IDictionary<string, object> properties)
        {
            foreach (var key in properties.Keys)
            {
                if (checkForKey.Equals(key))
                {
                    if (properties[checkForKey].Equals(checkForValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
