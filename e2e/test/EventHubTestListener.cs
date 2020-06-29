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
        private static readonly TimeSpan s_maximumWaitTime = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_lookbackTimeInMinutes = TimeSpan.FromMinutes(5);
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
            // empty private constructor, since we don't want external initialization of an instance
        }

        // verify required message is present in the dictionary
        public static bool VerifyIfMessageIsReceived(string deviceId, Client.Message message, string payload, string p1Value, TimeSpan? maxWaitTime = null)
        {
            if (!maxWaitTime.HasValue)
            {
                maxWaitTime = s_maximumWaitTime;
            }

            s_log.WriteLine($"Expected payload: deviceId={deviceId}; messageId = {message.MessageId}, userId={message.UserId}, payload={payload}; property1={p1Value}");

            bool isReceived = false;

            var sw = new Stopwatch();
            sw.Start();

            while (!isReceived && sw.Elapsed < maxWaitTime)
            {
                events.TryRemove(payload, out EventData eventData);
                if (eventData == null)
                {
                    continue;
                }

                isReceived = VerifyTestMessage(eventData, deviceId, message, p1Value);
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
            Stream bodyStream = eventData.GetBodyStream();

            var reader = new StreamReader(bodyStream);
            return reader.ReadToEnd();
#else
            return Encoding.UTF8.GetString(eventData.Body.ToArray());
#endif
        }

        private static bool VerifyTestMessage(EventData eventData, string deviceName, Client.Message message, string p1Value)
        {
#if NET451
            var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
#else
            var connectionDeviceId = eventData.Properties["iothub-connection-device-id"].ToString();
#endif
            Assert.AreEqual(deviceName, connectionDeviceId);
            Assert.IsTrue(VerifyKeyValue("property1", p1Value, eventData.Properties));
            Assert.IsTrue(VerifyKeyValue("property2", null, eventData.Properties));

            return true;
        }

        private static bool VerifyKeyValue(string checkForKey, string checkForValue, IDictionary<string, object> properties)
        {
            foreach (var key in properties.Keys)
            {
                if (checkForKey == key)
                {
                    // For http according to spec, expected value of property here is empty string
                    if ((null == checkForValue && string.IsNullOrEmpty((string)properties[checkForKey])) || checkForValue == ((string)properties[checkForKey]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
