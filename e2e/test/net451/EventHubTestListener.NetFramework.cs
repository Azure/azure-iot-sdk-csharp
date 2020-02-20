// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using QuotaExceededException = Microsoft.ServiceBus.Messaging.QuotaExceededException;

namespace Microsoft.Azure.Devices.E2ETests
{
    // EventHubListener Platform Adaptation Layer for .NET Framework.
    // This is using the WindowsAzure.ServiceBus NuGet dependency.
    public partial class EventHubTestListener
    {
        public static void CreateListenerPalAndReceiveMessages()
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, "messages/events");
            EventHubRuntimeInformation eventRuntimeInformation = eventHubClient.GetRuntimeInformation();
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            foreach (string partitionId in eventRuntimeInformation.PartitionIds)
            {
                try
                {
                    EventHubReceiver receiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partitionId, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
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
                catch (QuotaExceededException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListenerPalAndReceiveMessages)}: Cannot create receiver for partitionID {partitionId}: {ex}");
                }
            }
        }
    }
}
