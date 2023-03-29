﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class IotHub : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _hubConnectionString;
        private readonly IotHubTransportProtocol _transportProtocol;
        private readonly string _deviceId;

        private static readonly TimeSpan s_directMethodInvokeInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_desiredPropertiesSetInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_c2dMessagesSentInterval = TimeSpan.FromSeconds(3);

        private static IotHubServiceClient s_serviceClient;

        private long _totalMethodCallsCount = 0;
        private long _totalDesiredPropertiesUpdatesCount = 0;
        private long _totalC2dMessagesSentCount = 0;
        private long _totalFeedbackMessagesReceivedCount = 0;

        public IotHub(Logger logger, string hubConnectionString, string deviceId, IotHubTransportProtocol transportProtocol)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubConnectionString = hubConnectionString;
            _deviceId = deviceId;
            _transportProtocol = transportProtocol;
        }

        /// <summary>
        /// Initializes the service client.
        /// </summary>
        public void Initialize()
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = _transportProtocol,
            };
            s_serviceClient = new IotHubServiceClient(_hubConnectionString, options);
            _logger.Trace("Initialized a new service client instance.", TraceSeverity.Information);
        }

        public Task<string> GetEventHubCompatibleConnectionStringAsync(CancellationToken ct)
        {
            return s_serviceClient.GetEventHubCompatibleConnectionStringAsync(_hubConnectionString, ct);
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

                // Invoke the direct method asynchronously and get the response from the simulated device.
                DirectMethodClientResponse response = await s_serviceClient.DirectMethods.InvokeAsync(_deviceId, methodInvocation, ct);

                if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                {
                    _logger.Metric(
                        D2cDirectMethodDelaySeconds,
                        (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                }

                _logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);

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

                await s_serviceClient.Twins.UpdateAsync(_deviceId, twin, false, ct).ConfigureAwait(false);

                await Task.Delay(s_desiredPropertiesSetInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SendC2dMessagesAsync(CancellationToken ct)
        {
            await s_serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

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
                };

                _logger.Trace($"Sending message with Id {message.MessageId} to the device: {_deviceId}", TraceSeverity.Information);
                _logger.Metric(TotalC2dMessagesSentCount, _totalC2dMessagesSentCount);

                await s_serviceClient.Messages.SendAsync(_deviceId, message, ct).ConfigureAwait(false);

                await Task.Delay(s_c2dMessagesSentInterval, ct).ConfigureAwait(false);
            }

            await s_serviceClient.Messages.CloseAsync(ct).ConfigureAwait(false);
        }

        public async Task ReceiveMessageFeedbacksAsync(CancellationToken ct)
        {
            // It is important to note that receiver only gets feedback messages when the device is actively running and acting on messages.
            _logger.Trace("Starting to listen to cloud-to-device feedback messages", TraceSeverity.Verbose);

            AcknowledgementType OnC2dMessageAck(FeedbackBatch feedbackMessages)
            {
                foreach (FeedbackRecord feedbackRecord in feedbackMessages.Records)
                {
                    _logger.Trace(
                        $"Device {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}",
                        TraceSeverity.Information);
                }

                _totalFeedbackMessagesReceivedCount += feedbackMessages.Records.Count();
                _logger.Metric(TotalFeedbackMessagesReceivedCount, _totalFeedbackMessagesReceivedCount);

                return AcknowledgementType.Complete;
            }

            s_serviceClient.MessageFeedback.MessageFeedbackProcessor = OnC2dMessageAck;
            await s_serviceClient.MessageFeedback.OpenAsync(ct).ConfigureAwait(false);

            try
            {
                await Task.Delay(-1, ct);
            }
            catch (OperationCanceledException) { }

            await s_serviceClient.MessageFeedback.CloseAsync(ct).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();

            _logger.Trace($"IotHub instance disposed", TraceSeverity.Verbose);
        }
    }
}
