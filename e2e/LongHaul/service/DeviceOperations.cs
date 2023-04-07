// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class DeviceOperations
    {
        private readonly IotHubServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly Logger _logger;

        private long _totalMethodCallsCount = 0;
        private long _totalDesiredPropertiesUpdatesCount = 0;
        private long _totalC2dMessagesSentCount = 0;

        private static readonly TimeSpan s_directMethodInvokeInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_desiredPropertiesSetInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_c2dMessagesSentInterval = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(1);

        public DeviceOperations(IotHubServiceClient serviceClient, string deviceId, Logger logger)
        {
            _serviceClient = serviceClient;
            _deviceId = deviceId;
            _logger = logger;
            _logger.LoggerContext.Add("DeviceId", deviceId);
        }

        public async Task InvokeDirectMethodAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, DirectMethod);
            Stopwatch sw = new();
            while (!ct.IsCancellationRequested)
            {
                var payload = new CustomDirectMethodPayload
                {
                    RandomId = Guid.NewGuid(),
                    CurrentTimeUtc = DateTimeOffset.UtcNow,
                    MethodCallsCount = ++_totalMethodCallsCount,
                };

                var methodInvocation = new DirectMethodServiceRequest("EchoPayload")
                {
                    Payload = payload,
                    ResponseTimeout = TimeSpan.FromSeconds(30),
                };

                logger.Trace($"Invoking direct method for device: {_deviceId}", TraceSeverity.Information);
                logger.Metric(TotalDirectMethodCallsCount, _totalMethodCallsCount);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        sw.Restart();
                        // Invoke the direct method asynchronously and get the response from the simulated device.
                        DirectMethodClientResponse response = await _serviceClient.DirectMethods
                            .InvokeAsync(_deviceId, methodInvocation, ct)
                            .ConfigureAwait(false);
                        sw.Stop();
                        logger.Metric(DirectMethodRoundTripSeconds, sw.Elapsed.TotalSeconds);

                        if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                        {
                            logger.Metric(
                                D2cDirectMethodDelaySeconds,
                                (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                        }

                        logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);
                        break;
                    }
                    catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotOnline)
                    {
                        logger.Trace($"Caught exception invoking direct method.\n{ex}", TraceSeverity.Warning);
                        // retry
                    }
                    catch (Exception ex)
                    {
                        logger.Trace($"Unexpected exception observed while invoking direct method.\n{ex}");
                        break;
                    }

                    // retry delay
                    await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
                }

                // interval delay
                await Task.Delay(s_directMethodInvokeInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SetDesiredPropertiesAsync(string keyName, string properties, Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, SetDesiredProperties);
            Stopwatch sw = new();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = new ClientTwin();
                    twin.Properties.Desired[keyName] = properties;

                    ++_totalDesiredPropertiesUpdatesCount;
                    logger.Trace($"Updating the desired properties for device: {_deviceId}", TraceSeverity.Information);
                    logger.Metric(TotalDesiredPropertiesUpdatesCount, _totalDesiredPropertiesUpdatesCount);

                    sw.Restart();
                    await _serviceClient.Twins.UpdateAsync(_deviceId, twin, false, ct).ConfigureAwait(false);
                    sw.Stop();
                    logger.Metric(DesiredTwinUpdateRoundTripSeconds, sw.Elapsed.TotalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.Trace($"Unexpected exception observed while requesting twin property update.\n{ex}");
                }

                await Task.Delay(s_desiredPropertiesSetInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SendC2dMessagesAsync(Logger logger, CancellationToken ct)
        {
            await _serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);
            logger.LoggerContext.Add(OperationName, C2DMessage);
            Stopwatch sw = new();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var payload = new CustomC2dMessagePayload
                    {
                        RandomId = Guid.NewGuid(),
                        CurrentTimeUtc = DateTime.UtcNow,
                        MessagesSentCount = ++_totalC2dMessagesSentCount,
                    };
                    var message = new OutgoingMessage(payload)
                    {
                        // An acknowledgment is sent on delivery success or failure.
                        Ack = DeliveryAcknowledgement.Full,
                        MessageId = payload.RandomId.ToString(),
                    };

                    logger.Trace($"Sending message with Id {message.MessageId} to the device: {_deviceId}", TraceSeverity.Information);
                    logger.Metric(TotalC2dMessagesSentCount, _totalC2dMessagesSentCount);

                    sw.Restart();
                    await _serviceClient.Messages.SendAsync(_deviceId, message, ct).ConfigureAwait(false);
                    sw.Stop();
                    logger.Metric(C2dMessageRoundTripSeconds, sw.Elapsed.TotalSeconds);

                }
                catch (Exception ex)
                {
                    _logger.Trace($"Unexpected exception observed while sending a C2D message.\n{ex}");
                }

                await Task.Delay(s_c2dMessagesSentInterval, ct).ConfigureAwait(false);
            }
        }
    }
}
