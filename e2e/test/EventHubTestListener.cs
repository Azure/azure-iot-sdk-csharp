// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if !NET451
using Microsoft.Azure.EventHubs;
#else
using Microsoft.ServiceBus.Messaging;
using System.IO;
#endif

namespace Microsoft.Azure.Devices.E2ETests
{
    // Common code for EventHubListener.
    public partial class EventHubTestListener
    {
        private const int MaximumWaitTimeInMinutes = 5;
        private const int LookbackTimeInMinutes = 5;
        private const int OperationTimeoutInSeconds = 10;

        private static TestLogging s_log = TestLogging.GetInstance();
        private static ConcurrentDictionary<string, EventData> events = new ConcurrentDictionary<string, EventData>();

        public static Task<EventHubTestListener> CreateListener(string deviceName)
        {
            return CreateListenerPal(deviceName);
        }

        public async Task<bool> WaitForMessage(string deviceId, string payload, string p1Value)
        {
            bool isReceived = false;
            var sw = new Stopwatch();
            sw.Start();
            while (!isReceived && sw.Elapsed.TotalMinutes < 1)
            {
                if(!events.ContainsKey(payload))
                {
                    await ReceiveAsync().ConfigureAwait(false);
                }

                events.TryRemove(payload, out EventData eventData);
                if (eventData == null)
                {
                    continue;
                }

                isReceived = true;

                VerifyTestMessage(eventData, deviceId, payload, p1Value);
            }

            sw.Stop();

            return isReceived;
        }

        private static string GetEventDataBody(EventData eventData)
        {
#if NET451
            var bodyBytes = new byte[1024];
            int totalRead = 0;
            int read = 0;

            Stream bodyStream = eventData.GetBodyStream();
            do
            {
                read = bodyStream.Read(bodyBytes, totalRead, bodyBytes.Length - totalRead);
                totalRead += read;
            } while (read > 0 && (bodyBytes.Length - totalRead > 0));

            if (read > 0)
            {
                throw new InternalBufferOverflowException("EventHub message exceeded internal buffer.");
            }

            return Encoding.UTF8.GetString(bodyBytes, 0, totalRead);
#else
            return Encoding.UTF8.GetString(eventData.Body.ToArray());
#endif
        }

        private async Task ReceiveAsync()
        {
            IEnumerable<EventData> eventDatas = await _receiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(OperationTimeoutInSeconds)).ConfigureAwait(false);
            if (eventDatas == null)
            {
                s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: no events received.");
            }
            else
            {
                s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: {eventDatas.Count()} events received.");
                foreach (EventData eventData in eventDatas)
                {
                    string body = GetEventDataBody(eventData);
                    events[body] = eventData;
                }
            }

        }

        public Task CloseAsync()
        {
            return _receiver.CloseAsync();
        }

        private bool VerifyTestMessage(EventData eventData, string deviceName, string payload, string p1Value)
        {
#if NET451
            var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
#else
            var connectionDeviceId = eventData.Properties["iothub-connection-device-id"].ToString();
#endif
            Assert.AreEqual(deviceName, connectionDeviceId);
            Assert.IsTrue(VerifyKeyValue("property1", p1Value, eventData.Properties));

            return true;
        }

        private static bool VerifyKeyValue(string checkForKey, string checkForValue, IDictionary<string, object> properties)
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
    }
}
