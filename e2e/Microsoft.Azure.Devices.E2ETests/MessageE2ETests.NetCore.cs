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
    public partial class MessageE2ETests        
    {

        #region PAL
        internal async Task SendMessageThrottledForHttp()
        {
            // TODO: Update Jenkins Config
            if (Configuration.IoTHub.EventHubString.IsNullOrWhiteSpace())
            {
                return;
            }
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            EventHubClient eventHubClient;
            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1);

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, Client.TransportType.Http1);

            try
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)TestUtil.ShortRetryInMilliSec;
                await deviceClient.OpenAsync();

                string payload, p1Value;
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

                // Implementation of error injection of throttling on http is that it will throttle the
                // fault injection message itself only.  The duration of fault has no effect on http throttle.
                // Client is supposed to retry sending the throttling fault message until operation timeout.
                await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(TestUtil.FaultType_Throttle,
                    TestUtil.FaultCloseReason_Boom, TestUtil.DefaultDelayInSec, TestUtil.DefaultDurationInSec));
            }
            finally
            {
                await deviceClient.CloseAsync();
                await eventHubReceiver.CloseAsync();
                sequentialTestSemaphore.Release(1);
            }
        }
        internal async Task SendSingleMessage(Client.TransportType transport)
        {
            // TODO: Update Jenkins Config
            if (Configuration.IoTHub.EventHubString.IsNullOrWhiteSpace())
            {
                return;
            }
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

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
                    var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(30));
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

        internal async Task SendMessageRecovery(Client.TransportType transport,
            string faultType, string reason, int delayInSec, int durationInSec = 0, int retryDurationInMilliSec = 240000)
        {
            //ToDo: Update Config
            if (Configuration.IoTHub.EventHubString.IsNullOrWhiteSpace())
            {
                return;
            }

            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            PartitionReceiver eventHubReceiver = await CreateEventHubReceiver(deviceInfo.Item1);

            try
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;

                ConnectionStatus? lastConnectionStatus = null;
                ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
                int setConnectionStatusChangesHandlerCount = 0;

                deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
                {
                    lastConnectionStatus = status;
                    lastConnectionStatusChangeReason = statusChangeReason;
                    setConnectionStatusChangesHandlerCount++;
                });

                await deviceClient.OpenAsync();

                if (transport != Client.TransportType.Http1)
                {
                    Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                    Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
                }

                string payload, p1Value;
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

                // send error command and clear eventHubReceiver of the fault injection message
                await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason,
                    delayInSec,
                    durationInSec));

                await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));

                Thread.Sleep(1000);

                testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage);

                sw.Reset();
                sw.Start();
                while (!isReceived && sw.Elapsed.Minutes < 1)
                {
                    var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5));
                    isReceived = VerifyTestMessage(events, deviceInfo.Item1, payload, p1Value);
                }
                sw.Stop();

                await deviceClient.CloseAsync();

                if (transport != Client.TransportType.Http1)
                {
                    Assert.AreEqual(2, setConnectionStatusChangesHandlerCount);
                    Assert.AreEqual(ConnectionStatus.Disabled, lastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, lastConnectionStatusChangeReason);
                }
            }
            finally
            {
                await deviceClient.CloseAsync();
                await eventHubReceiver.CloseAsync();
                await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
                sequentialTestSemaphore.Release(1);
            }
        }
        #endregion

        #region Helper Functions
        private async Task<PartitionReceiver> CreateEventHubReceiver(string deviceName)
        {
            PartitionReceiver eventHubReceiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            string endpoint = Configuration.IoTHub.EventHubString;

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(endpoint);
            var eventRuntimeInformation = await eventHubClient.GetRuntimeInformationAsync();
            var eventHubPartitionsCount = eventRuntimeInformation.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.ConsumerGroup;

            while (eventHubReceiver == null && sw.Elapsed.Minutes < 1)
            {
                try
                {
                    eventHubReceiver = eventHubClient.CreateReceiver(consumerGroupName, partition, DateTime.Now.AddMinutes(-5));
                }
                catch (QuotaExceededException ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            sw.Stop();

            return eventHubReceiver;
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
                    if(properties[checkForKey].Equals(checkForValue))
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
