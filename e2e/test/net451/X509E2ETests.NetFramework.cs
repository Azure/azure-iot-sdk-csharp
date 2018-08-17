// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        #region PAL
        internal async Task SendSingleMessageX509(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            EventHubReceiver eventHubReceiver = await CreateEventHubReceiver(testDevice.Id).ConfigureAwait(false);

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

                _log.WriteLine($"Waiting for fault injection interval to finish {FaultInjection.DefaultDelayInSec}s.");
                await Task.Delay(FaultInjection.DefaultDurationInSec * 1000).ConfigureAwait(false);
            }
        }
        #endregion

        #region Helper Functions
        private async Task<EventHubReceiver> CreateEventHubReceiver(string deviceName)
        {
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, "messages/events");
            var eventHubPartitions = await eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
            var eventHubPartitionsCount = eventHubPartitions.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;
            return eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now, FaultInjection.EventHubEpoch++);
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
        #endregion
    }
}
