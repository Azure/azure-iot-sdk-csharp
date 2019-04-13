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
        private EventHubReceiver _receiver;
        private static Dictionary<string, EventHubReceiver> s_eventHubListenerCache = new Dictionary<string, EventHubReceiver>();
        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private EventHubTestListener(EventHubReceiver receiver)
        {
            _receiver = receiver;
        }

        public static async Task<EventHubTestListener> GetListenerAsync(string deviceName)
        {
            try
            {
                await s_semaphore.WaitAsync().ConfigureAwait(false);
                if (!s_eventHubListenerCache.TryGetValue(deviceName, out EventHubReceiver receiver))
                {
                    await CreateListenerPal(deviceName).ConfigureAwait(false);
                }

                EventHubReceiver ret = s_eventHubListenerCache[deviceName];

                s_log.WriteLine($"{nameof(GetListenerAsync)}: Using listener with partition ID: {ret.PartitionId} for device {deviceName}.");
                return new EventHubTestListener(ret);
            }
            finally
            {
                s_semaphore.Release();
            }
        }

        public static Task CreateListenerPal(string deviceName)
        {
            s_log.WriteLine($"{nameof(GetListenerAsync)}: Listener for device {deviceName} not found.");

            EventHubReceiver receiver = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, "messages/events");
            var eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
            string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, eventHubPartitionsCount);
            string consumerGroupName = Configuration.IoTHub.EventHubConsumerGroup;

            s_log.WriteLine($"{nameof(GetListenerAsync)}: Creating listner for device {deviceName}.");
            while (receiver == null && sw.Elapsed.Minutes < MaximumWaitTimeInMinutes)
            {
                try
                {
                    receiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now.AddMinutes(-LookbackTimeInMinutes));
                }
                catch (QuotaExceededException ex)
                {
                    s_log.WriteLine($"{nameof(EventHubTestListener)}.{nameof(CreateListener)}: Cannot create receiver: {ex}");
                }
            }

            sw.Stop();

            s_eventHubListenerCache[deviceName] = receiver;
            return Task.FromResult(0);
        }
    }
}
