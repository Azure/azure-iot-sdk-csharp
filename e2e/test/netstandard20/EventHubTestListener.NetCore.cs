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
        private PartitionReceiver _receiver;
        private static Dictionary<string, PartitionReceiver> s_eventHubListenerCache = new Dictionary<string, PartitionReceiver>();
        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private EventHubTestListener(PartitionReceiver receiver)
        {
            _receiver = receiver;
        }

        public static async Task<EventHubTestListener> GetListenerAsync(string deviceName)
        {
            try
            {
                await s_semaphore.WaitAsync().ConfigureAwait(false);
                if (!s_eventHubListenerCache.TryGetValue(deviceName, out PartitionReceiver receiver))
                {
                    await CreateListenerPal(deviceName).ConfigureAwait(false);
                }

                PartitionReceiver ret = s_eventHubListenerCache[deviceName];

                s_log.WriteLine($"{nameof(GetListenerAsync)}: Using listener with client ID: {ret.ClientId} for device {deviceName}.");
                return new EventHubTestListener(ret);
            }
            finally
            {
                s_semaphore.Release();
            }
        }

        public static async Task CreateListenerPal(string deviceName)
        {
            s_log.WriteLine($"{nameof(GetListenerAsync)}: Listener for device {deviceName} not found.");

            PartitionReceiver receiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var builder = new EventHubsConnectionStringBuilder(Configuration.IoTHub.EventHubString)
            {
                EntityPath = Configuration.IoTHub.EventHubCompatibleName
            };

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
            var eventRuntimeInformation = await eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
            var eventHubPartitionsCount = eventRuntimeInformation.PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            s_log.WriteLine($"{nameof(GetListenerAsync)}: Creating listner for device {deviceName}.");
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

            s_eventHubListenerCache[deviceName] = receiver;
        }
    }
}
