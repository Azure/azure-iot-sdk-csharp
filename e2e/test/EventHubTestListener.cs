﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !NET451
using Microsoft.Azure.EventHubs;
#else
using Microsoft.ServiceBus.Messaging;
#endif

namespace Microsoft.Azure.Devices.E2ETests
{
    // Common code for EventHubListener.
    public partial class EventHubTestListener
    {
        private static TestLogging s_log = TestLogging.GetInstance();

        public static Task<EventHubTestListener> CreateListener(string deviceName)
        {
            return CreateListenerPal(deviceName);
        }

        public async Task<bool> WaitForMessage(string deviceId, string payload, string p1Value)
        {
            bool isReceived = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!isReceived && sw.Elapsed.Minutes < 1)
            {
                var events = await _receiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                isReceived = VerifyTestMessage(events, deviceId, payload, p1Value);
            }

            sw.Stop();

            return isReceived;
        }

        public Task CloseAsync()
        {
            return _receiver.CloseAsync();
        }

        private bool VerifyTestMessage(IEnumerable<EventData> events, string deviceName, string payload, string p1Value)
        {
            if (events == null)
            {
                s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: no events received.");
                return false;
            }

            s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: {events.Count()} events received.");

            foreach (var eventData in events)
            {
                try
                {
#if !NET451
                    var data = Encoding.UTF8.GetString(eventData.Body.ToArray());
#else
                    var data = Encoding.UTF8.GetString(eventData.GetBytes());
#endif

                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: event data: '{data}'");

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
                catch (Exception ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: Cannot read eventData: {ex}");
                }
            }

            s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: none of the messages matched the expected payload '{payload}'.");

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
    }
}
