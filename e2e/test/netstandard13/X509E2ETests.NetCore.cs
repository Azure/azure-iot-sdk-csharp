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
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(testDevice.Id).ConfigureAwait(false);

            X509Certificate2 cert = Configuration.IoTHub.GetCertificateWithPrivateKey();

            var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);
            var deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, auth, transport);

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
                    isReceived = VerifyTestMessage(events, testDevice.Id, payload, p1Value);
                }

                sw.Stop();

                Assert.IsTrue(isReceived, "Message is not received.");
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await eventHubReceiver.CloseAsync().ConfigureAwait(false);
            }
        }
        #endregion

        #region Helper Functions
        private async Task<PartitionReceiver> CreateEventHubReceiver(string deviceName)
        {
            var builder = new EventHubsConnectionStringBuilder(Configuration.IoTHub.EventHubString)
            {
                EntityPath = Configuration.IoTHub.EventHubCompatibleName
            };

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());

            var eventHubRuntime = await eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
            var eventHubPartitionsCount = eventHubRuntime.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;
            return eventHubClient.CreateReceiver(consumerGroupName, partition, DateTime.Now.AddMinutes(-5));
        }

        private bool VerifyTestMessage(IEnumerable<EventData> events, string deviceName, string payload, string p1Value)
        {
            foreach (var eventData in events)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.ToArray());
                if (data == payload)
                {
                    var connectionDeviceId = eventData.Properties["iothub-connection-device-id"].ToString();
                    if (string.Equals(connectionDeviceId, deviceName, StringComparison.CurrentCultureIgnoreCase) &&
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
                if (checkForKey == key)
                {
                    if ((string)properties[checkForKey] == checkForValue)
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
