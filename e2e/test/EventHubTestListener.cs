// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;

#if !NET451
using Microsoft.Azure.EventHubs;
#else
using Microsoft.ServiceBus.Messaging;
using System.IO;
#endif

namespace Microsoft.Azure.Devices.E2ETests
{
    public sealed partial class EventHubTestListener
    {
        private const int MaximumWaitTimeInMinutes = 1;
        private const int LookbackTimeInMinutes = 5;
        private const int OperationTimeoutInSeconds = 10;

        private static TestLogging s_log = TestLogging.GetInstance();
        private static ConcurrentDictionary<string, EventData> events = new ConcurrentDictionary<string, EventData>();

        static EventHubTestListener()
        {
            // create the receiver pool - different for netfm and netcore
            // start receiving messages and store in a dictionary
            CreateListenerPalAndReceiveMessages();
        }

        private EventHubTestListener()
        {
            // empty private constructor, since we don't want external intilization of an instance
        }

        // verify required message is present in the dictionary
        public static bool VerifyIfMessageIsReceived(string deviceId, string payload, string p1Value)
        {
            s_log.WriteLine($"Expected payload: deviceId={deviceId}; payload={payload}; property1={p1Value}");

            bool isReceived = false;

            var sw = new Stopwatch();
            sw.Start();

            while (!isReceived && sw.Elapsed.TotalMinutes < MaximumWaitTimeInMinutes)
            {
                events.TryRemove(payload, out EventData eventData);
                if (eventData == null)
                {
                    continue;
                }

                isReceived = VerifyTestMessage(eventData, deviceId, p1Value);
            }

            sw.Stop();

            return isReceived;
        }

        private static void ProcessEventData(IEnumerable<EventData> eventDatas)
        {
            if (eventDatas == null)
            {
                s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(ProcessEventData)}: no events received.");
            }
            else
            {
                s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(ProcessEventData)}: {eventDatas.Count()} events received.");
                foreach (EventData eventData in eventDatas)
                {
                    string body = GetEventDataBody(eventData);
                    events[body] = eventData;
                }
            }
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

        private static bool VerifyTestMessage(EventData eventData, string deviceName, string p1Value)
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
