// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region PAL
        //// This function create a device with x509 cert and send a message to the iothub on the transport specified.
        //// It then verifies the message is received at the eventHubClient.
        internal async Task SendSingleMessageX509(Client.TransportType transport)
        {
            // TODO: Update Jenkins Config
            string endpoint = Configuration.IoTHub.EventHubString;
            if (endpoint.IsNullOrWhiteSpace())
            {
                return;
            }

            Tuple<string, string> deviceInfo = TestUtil.CreateDeviceWithX509(DevicePrefix, hostName, registryManager);

            EventHubClient eventHubClient;
            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1).ConfigureAwait(false);

            X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

            var auth = new DeviceAuthenticationWithX509Certificate(deviceInfo.Item1, cert);
            var deviceClient = DeviceClient.Create(deviceInfo.Item2, auth, transport);

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                string payload;
                string p1Value;
                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = false;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!isReceived && sw.Elapsed.Minutes < 1)
                {
                    var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    isReceived = VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);
                }

                sw.Stop();

                Assert.IsTrue(isReceived, "Message is not received.");
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await eventHubReceiver.CloseAsync().ConfigureAwait(false);
                await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager).ConfigureAwait(false);
            }
        }
        #endregion

        #region Helper Functions
        private async Task<PartitionReceiver> CreateEventHubReceiver(string deviceName)
        {
            string endpoint = Configuration.IoTHub.EventHubString;
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(endpoint);
            var eventHubRuntime = await eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
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
        #endregion
    }
}