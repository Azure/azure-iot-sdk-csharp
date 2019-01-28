// Copyright (c) Microsoft. All rights reserved.
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
                IEnumerable<EventData> events = await _receiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(OperationTimeoutInSeconds)).ConfigureAwait(false);
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
                    string data = Encoding.UTF8.GetString(eventData.Body.ToArray());
#else
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

                    string data = Encoding.UTF8.GetString(bodyBytes, 0, totalRead);
#endif

                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(VerifyTestMessage)}: event data: '{data}'");

                    if (data == payload)
                    {
#if !NET451
                        var connectionDeviceId = eventData.Properties["iothub-connection-device-id"].ToString();
#else
                        var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
#endif
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
