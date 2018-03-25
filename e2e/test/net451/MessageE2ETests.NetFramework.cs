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
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = CreateEventHubReceiver(deviceInfo.Item1, out eventHubClient);

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

        internal async Task SendMessageRecovery(Client.TransportType transport,
            string faultType, string reason, int delayInSec, int durationInSec = 0, int retryDurationInMilliSec = 240000)
        {
            await sequentialTestSemaphore.WaitAsync();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = CreateEventHubReceiver(deviceInfo.Item1, out eventHubClient);

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

        internal async Task SendSingleMessage(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            EventHubClient eventHubClient;
            EventHubReceiver eventHubReceiver = CreateEventHubReceiver(deviceInfo.Item1, out eventHubClient);
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
        #endregion

        #region Helper Functions
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

        private EventHubReceiver CreateEventHubReceiver(string deviceName, out EventHubClient eventHubClient)
        {
            EventHubReceiver eventHubReceiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            eventHubClient = EventHubClient.CreateFromConnectionString(hubConnectionString, "messages/events");
            var eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.ConsumerGroup;

            while (eventHubReceiver == null && sw.Elapsed.Minutes < 1)
            {
                try
                {
                    eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now, TestUtil.EventHubEpoch++);
                }
                catch (QuotaExceededException ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            sw.Stop();

            return eventHubReceiver;
        }
        #endregion
    }
}