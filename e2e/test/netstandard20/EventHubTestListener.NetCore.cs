// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    // EventHubListener Platform Adaptation Layer for .NET Standard
    // This is using the new Microsoft.Azure.EventHubs from https://github.com/Azure/azure-event-hubs
    public partial class EventHubTestListener
    {
        private PartitionReceiver _receiver;

        private EventHubTestListener(PartitionReceiver receiver)
        {
            _receiver = receiver;
        }

        public static async Task<EventHubTestListener> CreateListenerPal(string deviceName, bool usePrimaryHub)
        {
            PartitionReceiver receiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var eventHubString = usePrimaryHub ? Configuration.IoTHub.EventHubString : Configuration.IoTHub.EventHubStringSecondary;
            var eventHubCompatibleName = usePrimaryHub ? Configuration.IoTHub.EventHubCompatibleName : Configuration.IoTHub.EventHubCompatibleNameSecondary;
            string consumerGroupName = usePrimaryHub ? Configuration.IoTHub.EventHubConsumerGroup : Configuration.IoTHub.EventHubConsumerGroupSecondary;

            var builder = new EventHubsConnectionStringBuilder(eventHubString)
            {
                EntityPath = eventHubCompatibleName
            };

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            var eventRuntimeInformation = await eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
            var eventHubPartitionsCount = eventRuntimeInformation.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);

            while (receiver == null && sw.Elapsed.TotalMinutes < MaximumWaitTimeInMinutes)
            {
                try
                {
                    receiver = eventHubClient.CreateReceiver(consumerGroupName, partition, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
                }
                catch (EventHubsException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListener)}: Cannot create receiver: {ex}");
                }
            }

            sw.Stop();

            return new EventHubTestListener(receiver);
        }
    }
}
