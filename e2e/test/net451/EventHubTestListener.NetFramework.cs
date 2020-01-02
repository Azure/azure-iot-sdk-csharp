// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using QuotaExceededException = Microsoft.ServiceBus.Messaging.QuotaExceededException;

    // EventHubListener Platform Adaptation Layer for .NET Framework.
    // This is using the WindowsAzure.ServiceBus NuGet dependency.
    public partial class EventHubTestListener
    {
        public static void CreateListenerPalAndReceiveMessages()
        {
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, "messages/events");
            var eventRuntimeInformation = eventHubClient.GetRuntimeInformation();
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            foreach (string partitionId in eventRuntimeInformation.PartitionIds)
            {
                try
                {
                    EventHubReceiver receiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partitionId, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
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
                catch (QuotaExceededException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListenerPalAndReceiveMessages)}: Cannot create receiver for partitionID {partitionId}: {ex}");
                }
            }
        }
    }
}
