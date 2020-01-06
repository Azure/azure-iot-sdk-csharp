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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    // EventHubListener Platform Adaptation Layer for .NET Standard
    // This is using the new Microsoft.Azure.EventHubs from https://github.com/Azure/azure-event-hubs
    public partial class EventHubTestListener
    {
        public static void CreateListenerPalAndReceiveMessages()
        {
            var builder = new EventHubsConnectionStringBuilder(Configuration.IoTHub.EventHubString)
            {
                EntityPath = Configuration.IoTHub.EventHubCompatibleName
            };

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            var eventRuntimeInformation = eventHubClient.GetRuntimeInformationAsync().Result;
            var eventHubPartitionsCount = eventRuntimeInformation.PartitionCount;
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            foreach (string partitionId in eventRuntimeInformation.PartitionIds)
            {
                try
                {
                    PartitionReceiver receiver = eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
                    s_log.WriteLine($"EventHub receiver created for partition {partitionId}, listening from {LookbackTimeInMinutes}");

                    new Thread(
                        async () => {
                            while (true)
                            {
                                IEnumerable<EventData> eventDatas = await receiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(OperationTimeoutInSeconds)).ConfigureAwait(false);
                                if (eventDatas == null)
                                {
                                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListenerPalAndReceiveMessages)}: no events received.");
                                }
                                else
                                {
                                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListenerPalAndReceiveMessages)}: {eventDatas.Count()} events received.");
                                    foreach (EventData eventData in eventDatas)
                                    {
                                        string body = GetEventDataBody(eventData);
                                        events[body] = eventData;
                                    }
                                }
                            }
                        }).Start();
                }
                catch (EventHubsException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListenerPalAndReceiveMessages)}: Cannot create receiver for partitionID {partitionId}: {ex}");
                }
            }


        }
    }
}
