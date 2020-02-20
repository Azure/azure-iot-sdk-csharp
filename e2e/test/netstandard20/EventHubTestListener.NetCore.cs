// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

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

            var eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            EventHubRuntimeInformation eventRuntimeInformation = eventHubClient.GetRuntimeInformationAsync().Result;
            int eventHubPartitionsCount = eventRuntimeInformation.PartitionCount;
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            foreach (string partitionId in eventRuntimeInformation.PartitionIds)
            {
                try
                {
                    PartitionReceiver receiver = eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
                    s_log.WriteLine($"EventHub receiver created for partition {partitionId}, listening from {LookbackTimeInMinutes}");

                    new Task(async () =>
                    {
                        while (true)
                        {
                            IEnumerable<EventData> eventDatas = await receiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(OperationTimeoutInSeconds)).ConfigureAwait(false);
                            ProcessEventData(eventDatas);
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
