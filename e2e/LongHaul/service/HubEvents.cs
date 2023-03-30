// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Mash.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class HubEvents
    {
        private readonly IotHub _iotHub;
        private readonly Logger _logger;

        internal HubEvents(IotHub serviceClient, Logger logger)
        {
            _iotHub = serviceClient;
            _logger = logger;
        }

        internal async Task RunAsync(CancellationToken ct)
        {
            var readEventOptions = new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(3) };
            string eventHubConnectionString = await _iotHub.GetEventHubCompatibleConnectionStringAsync(ct);
            await using var eventHubClient = new EventHubConsumerClient(
                "$Default",
                eventHubConnectionString);

            while (!ct.IsCancellationRequested)
            {
                await foreach (PartitionEvent partitionEvent in eventHubClient.ReadEventsAsync(readEventOptions, ct))
                {
                    if (partitionEvent.Data == null)
                    {
                        await Task.Delay(5000, ct).ConfigureAwait(false);
                        break;
                    }

                    try
                    {
                        DeviceEventSystemProperties eventMetadata = JsonConvert.DeserializeObject<DeviceEventSystemProperties>(
                            JsonConvert.SerializeObject(partitionEvent.Data.SystemProperties));

                        switch (eventMetadata.MessageSource)
                        {
                            case DeviceEventMessageSource.DeviceConnectionStateEvents:
                                DeviceEventProperties deviceEvent = JsonConvert.DeserializeObject<DeviceEventProperties>(
                                    JsonConvert.SerializeObject(partitionEvent.Data.Properties));
                                TimeSpan timeSince = DateTimeOffset.UtcNow - deviceEvent.OperationOnUtc;
                                if (timeSince.TotalSeconds < 30)
                                {
                                    _logger.Trace($"{deviceEvent.HubName}/{deviceEvent.DeviceId} has event {deviceEvent.OperationType} at {deviceEvent.OperationOnUtc.LocalDateTime}, {timeSince} ago.", TraceSeverity.Information);
                                }
                                else
                                {
                                    _logger.Trace($"HubEvents: known message source {eventMetadata.MessageSource}", TraceSeverity.Verbose);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Failed to convert event [{JsonConvert.SerializeObject(partitionEvent.Data.Properties)}] to DeviceEventProperties due to {ex}", TraceSeverity.Warning);
                    }
                }
            }
        }
    }
}
