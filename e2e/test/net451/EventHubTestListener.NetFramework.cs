﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using QuotaExceededException = Microsoft.Azure.Devices.Client.Exceptions.QuotaExceededException;

    // EventHubListener Platform Adaptation Layer for .NET Framework.
    // This is using the WindowsAzure.ServiceBus NuGet dependency.
    public partial class EventHubTestListener
    {
        private EventHubReceiver _receiver;
        private static int s_eventHubEpoch = 0;
        private static object s_eventHubEpochLock = new object();

        private EventHubTestListener(EventHubReceiver receiver)
        {
            _receiver = receiver;
        }

        public static async Task<EventHubTestListener> CreateListenerPal(string deviceName)
        {
            EventHubReceiver receiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, "messages/events");
            var eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            while (receiver == null && sw.Elapsed.Minutes < 1)
            {
                try
                {
                    lock (s_eventHubEpochLock)
                    {
                        receiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now, s_eventHubEpoch++);
                    }
                }
                catch (QuotaExceededException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListener)}: Cannot create receiver: {ex}");
                }
            }

            sw.Stop();

            return new EventHubTestListener(receiver);
        }
    }
}
