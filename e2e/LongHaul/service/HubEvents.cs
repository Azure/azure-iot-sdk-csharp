// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Mash.Logging;

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
            var options = new JsonSerializerOptions { Converters = { new ReadOnlyMemoryConverter() } };

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

                    DeviceEventSystemProperties eventMetadata;
                    try
                    {
                        eventMetadata = JsonSerializer.Deserialize<DeviceEventSystemProperties>(
                            JsonSerializer.Serialize(partitionEvent.Data.SystemProperties, options),
                            options);

                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Failed to convert event [{JsonSerializer.Serialize(partitionEvent.Data.SystemProperties)}] to DeviceEventProperties due to {ex}", TraceSeverity.Warning);
                        continue;
                    }

                    try
                    {
                        switch (eventMetadata.MessageSource)
                        {
                            case DeviceEventMessageSource.DeviceConnectionStateEvents:
                                DeviceEventProperties deviceEvent = JsonSerializer.Deserialize<DeviceEventProperties>(
                                    JsonSerializer.Serialize(partitionEvent.Data.Properties, options),
                                    options);
                                TimeSpan timeSince = DateTimeOffset.UtcNow - deviceEvent.OperationOnUtc;
                                if (timeSince.TotalSeconds < 30)
                                {
                                    _logger.Trace($"{deviceEvent.HubName}/{deviceEvent.DeviceId} has event {deviceEvent.OperationType} at {deviceEvent.OperationOnUtc.LocalDateTime}, {timeSince} ago.", TraceSeverity.Information);
                                }
                                else
                                {
                                    _logger.Trace($"HubEvents: known message source {eventMetadata.MessageSource}/{deviceEvent.OperationType} {timeSince} ago.", TraceSeverity.Verbose);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Failed to convert event [{JsonSerializer.Serialize(partitionEvent.Data.Properties)}] to DeviceEventProperties due to {ex}", TraceSeverity.Warning);
                        continue;
                    }
                }
            }
        }

        private class ReadOnlyMemoryConverter : JsonConverter<ReadOnlyMemory<byte>>
        {
            public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new();

            public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
        }
    }
}
