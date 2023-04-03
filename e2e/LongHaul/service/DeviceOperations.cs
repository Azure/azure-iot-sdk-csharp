// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Threading;
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

        public DeviceOperations(IotHubServiceClient serviceClient, string deviceId, Logger logger)
        {
            _serviceClient = serviceClient;
            _deviceId = deviceId;
            _logger = logger;
            _logger.LoggerContext.Add("DeviceId", deviceId);
        }

        public async Task InvokeDirectMethodAsync(CancellationToken ct)
        {
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

                _logger.Trace($"Invoking direct method for device: {_deviceId}", TraceSeverity.Information);
                _logger.Metric(TotalDirectMethodCallsCount, _totalMethodCallsCount);

                try
                {
                    // Invoke the direct method asynchronously and get the response from the simulated device.
                    DirectMethodClientResponse response = await _serviceClient.DirectMethods.InvokeAsync(_deviceId, methodInvocation, ct);

                    if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                    {
                        _logger.Metric(
                            D2cDirectMethodDelaySeconds,
                            (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                    }

                    _logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);
                }
                catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotOnline)
                {
                    _logger.Trace($"Caught exception invoking direct method {ex}", TraceSeverity.Warning);
                }

                await Task.Delay(s_directMethodInvokeInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SetDesiredPropertiesAsync(string keyName, string properties, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var twin = new ClientTwin();
                twin.Properties.Desired[keyName] = properties;

                ++_totalDesiredPropertiesUpdatesCount;
                _logger.Trace($"Updating the desired properties for device: {_deviceId}", TraceSeverity.Information);
                _logger.Metric(TotalDesiredPropertiesUpdatesCount, _totalDesiredPropertiesUpdatesCount);

                await _serviceClient.Twins.UpdateAsync(_deviceId, twin, false, ct).ConfigureAwait(false);

                await Task.Delay(s_desiredPropertiesSetInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SendC2dMessagesAsync(CancellationToken ct)
        {
            await _serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            while (!ct.IsCancellationRequested)
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

                _logger.Trace($"Sending message with Id {message.MessageId} to the device: {_deviceId}", TraceSeverity.Information);
                _logger.Metric(TotalC2dMessagesSentCount, _totalC2dMessagesSentCount);

                await _serviceClient.Messages.SendAsync(_deviceId, message, ct).ConfigureAwait(false);

                await Task.Delay(s_c2dMessagesSentInterval, ct).ConfigureAwait(false);
            }
        }
    }
}
