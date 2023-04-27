// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.LongHaul.Device.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal sealed class IotHub : IAsyncDisposable
    {
        private readonly string _deviceConnectionString;
        private readonly IotHubClientOptions _clientOptions;
        private readonly Logger _logger;

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        private volatile int _connectionStatusChangeCount = 0;
        private readonly Stopwatch _disconnectedTimer = new();
        private ConnectionStatus _disconnectedStatus;
        private ConnectionStatusChangeReason _disconnectedReason;
        private RecommendedAction _disconnectedRecommendedAction;
        private volatile IotHubDeviceClient _deviceClient;
        private CancellationToken _ct;

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_deviceTwinUpdateInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_fileUploadInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(1);
        private readonly ConcurrentQueue<TelemetryMessage> _messagesToSend = new();

        private long _totalTelemetryMessagesSent = 0;
        private long _totalTwinUpdatesReported = 0;
        private long _totalTwinCallbacksHandled = 0;
        private long _totalDesiredPropertiesHandled = 0;
        private long _totalC2dMessagesCompleted = 0;
        private long _totalC2dMessagesRejected = 0;

        public IotHub(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceConnectionString = parameters.ConnectionString;
            _clientOptions = new IotHubClientOptions(parameters.GetTransportSettings())
            {
                PayloadConvention = parameters.GetPayloadConvention(),
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            _deviceClient = null;
        }

        public bool IsConnected => _deviceClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected;

        public Dictionary<string, string> TelemetryUserProperties { get; } = new();

        /// <summary>
        /// Initializes the connection to IoT Hub.
        /// </summary>
        public async Task InitializeAsync(CancellationToken? ct = null)
        {
            if (ct != null)
            {
                _ct = ct.Value;
            }

            await _lifetimeControl.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_deviceClient == null)
                {
                    _deviceClient = new IotHubDeviceClient(_deviceConnectionString, _clientOptions)
                    {
                        ConnectionStatusChangeCallback = ConnectionStatusChangesHandlerAsync
                    };
                }
                else
                {
                    await _deviceClient.CloseAsync().ConfigureAwait(false);
                }

                await _deviceClient.OpenAsync().ConfigureAwait(false);
                await _deviceClient.SetDirectMethodCallbackAsync(DirectMethodCallback).ConfigureAwait(false);
                await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallbackAsync).ConfigureAwait(false);
                await _deviceClient.SetIncomingMessageCallbackAsync(OnC2dMessageReceivedAsync).ConfigureAwait(false);
            }
            finally
            {
                _lifetimeControl.Release();
            }
        }

        /// <summary>
        /// Frequently send telemetry messages to the hub.
        /// </summary>
        /// <param name="ct">The cancellation token</param>
        public async Task SendTelemetryMessagesAsync(Logger logger, CancellationToken ct)
        {
            // AMQP supports bulk telemetry sending, so we'll configure how many to send at a time.
            int maxBulkMessages = _clientOptions.TransportSettings is IotHubClientAmqpSettings
                ? 10
                : 1;

            // We want to test both paths for AMQP so we'll use a boolean to alternate between bulk and single.
            bool sendSingle = true;

            var pendingMessages = new List<TelemetryMessage>(maxBulkMessages);
            logger.LoggerContext.Add(OperationName, LoggingConstants.TelemetryMessage);
            var sw = new Stopwatch();

            while (!ct.IsCancellationRequested)
            {
                logger.Metric(MessageBacklog, _messagesToSend.Count);

                await Task.Delay(s_messageLoopSleepTime, ct).ConfigureAwait(false);

                // Get messages to send, unless we're retrying a previous set of messages.
                if (!pendingMessages.Any())
                {
                    // Pull some number of messages from the queue, or until empty.
                    for (int i = 0; i < maxBulkMessages; ++i)
                    {
                        if (sendSingle && pendingMessages.Count == 1)
                        {
                            break;
                        }

                        _messagesToSend.TryDequeue(out TelemetryMessage pendingMessage);
                        if (pendingMessage == null)
                        {
                            break;
                        }

                        pendingMessages.Add(pendingMessage);
                    }
                }

                // Send any message prepped to send.
                if (pendingMessages.Any())
                {
                    try
                    {
                        if (pendingMessages.Count > 1)
                        {
                            logger.Trace($"Sending {pendingMessages.Count} telemetry messages in bulk.", TraceSeverity.Information);
                            sw.Restart();
                            await _deviceClient.SendTelemetryAsync(pendingMessages, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            logger.Trace("Sending a telemetry message.", TraceSeverity.Information);
                            sw.Restart();
                            await _deviceClient.SendTelemetryAsync(pendingMessages.First(), ct).ConfigureAwait(false);
                        }

                        sw.Stop();

                        _totalTelemetryMessagesSent += pendingMessages.Count;
                        _logger.Metric(TotalTelemetryMessagesSent, _totalTelemetryMessagesSent);
                        _logger.Metric(TelemetryMessageDelaySeconds, sw.Elapsed.TotalSeconds);
                        pendingMessages.Clear();

                        // Alternate sending between single and in bulk.
                        sendSingle = !sendSingle;
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Exception when sending telemetry when connected is {IsConnected}\n{ex}", TraceSeverity.Warning);
                    }
                }
            }
        }

        public void AddTelemetry(
            TelemetryBase telemetryObject,
            IDictionary<string, string> extraProperties = null)
        {
            Debug.Assert(_deviceClient != null);
            Debug.Assert(telemetryObject != null);

            var iotMessage = new TelemetryMessage(telemetryObject)
            {
                // Add the event time to the system property
                CreatedOnUtc = telemetryObject.EventDateTimeUtc ?? DateTime.UtcNow,
            };

            foreach (KeyValuePair<string, string> prop in TelemetryUserProperties)
            {
                iotMessage.Properties.TryAdd(prop.Key, prop.Value);
            }

            if (extraProperties != null)
            {
                foreach (KeyValuePair<string, string> prop in extraProperties)
                {
                    // Use TryAdd to ensure the attempt does not fail with an exception
                    // in the event that this key already exists in this dictionary,
                    // in which case it'll log an error.
                    if (!iotMessage.Properties.TryAdd(prop.Key, prop.Value))
                    {
                        _logger.Trace($"Could not add telemetry property {prop.Key} due to conflict.", TraceSeverity.Error);
                    }
                }
            }

            // Worker feeding off this queue will dispose the messages when they are sent
            _messagesToSend.Enqueue(iotMessage);
        }

        public async Task ReportReadOnlyPropertiesAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, ReportTwinProperties);
            var sw = new Stopwatch();

            while (!ct.IsCancellationRequested)
            {
                sw.Restart();
                await SetPropertiesAsync("totalTelemetryMessagesSent", _totalTelemetryMessagesSent, logger, ct).ConfigureAwait(false);
                sw.Stop();

                ++_totalTwinUpdatesReported;
                logger.Metric(TotalTwinUpdatesReported, _totalTwinUpdatesReported);
                logger.Metric(ReportedTwinUpdateOperationSeconds, sw.Elapsed.TotalSeconds);

                await Task.Delay(s_deviceTwinUpdateInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SetPropertiesAsync<T>(string keyName, T properties, Logger logger, CancellationToken ct)
        {
            Debug.Assert(_deviceClient != null);
            Debug.Assert(properties != null);

            var reportedProperties = new ReportedProperties
            {
                { keyName, properties },
            };
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    long version = await _deviceClient
                        .UpdateReportedPropertiesAsync(reportedProperties, ct)
                        .ConfigureAwait(false);

                    logger.Trace($"Set the reported property with key {keyName} and value {properties} in device twin; observed version {version}.", TraceSeverity.Information);
                    break;
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception reporting property\n{ex}", TraceSeverity.Warning);
                    await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
                }
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    TwinProperties twin = await _deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
                    if (!twin.Reported.TryGetValue<T>(keyName, out T actualValue))
                    {
                        logger.Trace($"Couldn't find the reported property {keyName} in the device twin.", TraceSeverity.Warning);
                    }
                    else if (!actualValue.Equals(properties))
                    {
                        logger.Trace($"Couldn't validate value for {keyName} was set to {properties}, found {actualValue}.");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception getting twin\n{ex}", TraceSeverity.Warning);
                    await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
                }
            }
        }

        public async Task UploadFilesAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, UploadFiles);
            var sw = new Stopwatch();

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(s_fileUploadInterval, ct).ConfigureAwait(false);

                string fileName = $"TestPayload-{Guid.NewGuid()}.txt";
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes("TestPayload"));

                var fileUploadSasUriRequest = new FileUploadSasUriRequest(fileName);
                FileUploadSasUriResponse sasUri;

                try
                {
                    sasUri = await _deviceClient
                        .GetFileUploadSasUriAsync(fileUploadSasUriRequest, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception getting file upload SAS URI\n{ex}", TraceSeverity.Warning);
                    continue;
                }

                Uri uploadUri = sasUri.GetBlobUri();
                var fileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, false)
                {
                    StatusCode = 500,
                };

                try
                {
                    logger.Trace($"Attempting to upload {fileName}...", TraceSeverity.Information);
                    var blockBlobClient = new BlockBlobClient(uploadUri);
                    await blockBlobClient.UploadAsync(ms, new BlobUploadOptions(), ct).ConfigureAwait(false);

                    logger.Trace("Succeeded uploading file to Azure Storage", TraceSeverity.Information);
                    fileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, true)
                    {
                        StatusCode = 200,
                    };
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception uploading blob\n{ex}", TraceSeverity.Warning);
                }

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        sw.Restart();
                        await _deviceClient.CompleteFileUploadAsync(fileUploadCompletionNotification, ct).ConfigureAwait(false);
                        sw.Stop();
                        logger.Metric(FileUploadOperationSeconds, sw.Elapsed.Seconds);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Trace($"Exception completing file upload\n{ex}", TraceSeverity.Warning);
                        await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace($"Disposing the {nameof(IotHub)} instance", TraceSeverity.Verbose);

            if (_lifetimeControl != null)
            {
                _lifetimeControl.Dispose();
                _lifetimeControl = null;
            }

            await _deviceClient.DisposeAsync().ConfigureAwait(false);

            _logger.Trace($"{nameof(IotHub)} instance disposed", TraceSeverity.Verbose);

        }

        private async void ConnectionStatusChangesHandlerAsync(ConnectionStatusInfo connectionInfo)
        {
            ConnectionStatus status = connectionInfo.Status;
            ConnectionStatusChangeReason reason = connectionInfo.ChangeReason;
            RecommendedAction recommendedAction = connectionInfo.RecommendedAction;

            string eventName = status == ConnectionStatus.Connected
                ? ConnectedEvent
                : DiscconnectedEvent;

            _logger.Event(
                eventName,
                new Dictionary<string, string>
                {
                    { ConnectionReason, reason.ToString() },
                    { ConnectionRecommendedAction, recommendedAction.ToString() },
                });

            _logger.Trace(
                $"Connection status changed ({++_connectionStatusChangeCount}): status=[{status}], reason=[{reason}], recommendation=[{recommendedAction}]",
                TraceSeverity.Information);

            if (IsConnected)
            {
                // The device client has connected.
                if (_disconnectedTimer.IsRunning)
                {
                    _disconnectedTimer.Stop();
                    _logger.Metric(
                        DisconnectedDurationSeconds,
                        _disconnectedTimer.Elapsed.TotalSeconds,
                        new Dictionary<string, string>
                        {
                            { DisconnectedStatus, _disconnectedStatus.ToString() },
                            { DisconnectedReason, _disconnectedReason.ToString() },
                            { DisconnectedRecommendedAction, _disconnectedRecommendedAction.ToString() },
                            { ConnectionStatusChangeCount, _connectionStatusChangeCount.ToString() },
                        });
                }
            }
            else if (!IsConnected
                && !_disconnectedTimer.IsRunning)
            {
                _disconnectedTimer.Restart();
                _disconnectedStatus = status;
                _disconnectedReason = reason;
                _disconnectedRecommendedAction = recommendedAction;
            }

            switch (connectionInfo.RecommendedAction)
            {
                case RecommendedAction.OpenConnection:
                    _logger.Trace($"Following recommended action of reinitializing the client.", TraceSeverity.Information);
                    while (!_ct.IsCancellationRequested)
                    {
                        try
                        {
                            await InitializeAsync().ConfigureAwait(false);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Trace($"Exception re-initializing client\n{ex}", TraceSeverity.Warning);
                            await Task.Delay(s_retryInterval, _ct).ConfigureAwait(false);
                        }
                    }
                    break;

                case RecommendedAction.PerformNormally:
                    _logger.Trace("The client has retrieved twin values after the connection status changes into CONNECTED.", TraceSeverity.Information);
                    break;

                case RecommendedAction.WaitForRetryPolicy:
                    _logger.Trace("Letting the client retry.", TraceSeverity.Information);
                    break;

                case RecommendedAction.Quit:
                    _logger.Trace("Quitting.", TraceSeverity.Information);
                    break;
            }
        }

        private Task<DirectMethodResponse> DirectMethodCallback(DirectMethodRequest methodRequest)
        {
            _logger.Trace($"Received direct method [{methodRequest.MethodName}] with payload [{Encoding.UTF8.GetString(methodRequest.GetPayloadAsBytes())}].", TraceSeverity.Information);

            switch (methodRequest.MethodName)
            {
                case "EchoPayload":
                    try
                    {
                        if (methodRequest.TryGetPayload(out CustomDirectMethodPayload methodPayload))
                        {
                            _logger.Trace($"Echoing back the payload of direct method.", TraceSeverity.Information);
                            _logger.Metric(
                                C2dDirectMethodDelaySeconds,
                                (DateTimeOffset.UtcNow - methodPayload.SentOnUtc).TotalSeconds);

                            // Log the current time again and send the response back to the service app.
                            methodPayload.SentOnUtc = DateTimeOffset.UtcNow;
                            return Task.FromResult(new DirectMethodResponse(200) { Payload = methodPayload });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"Failed to parse the payload for direct method {methodRequest.MethodName} due to {ex}.", TraceSeverity.Error);
                        return Task.FromResult(new DirectMethodResponse(400) { Payload = ex.Message });
                    }
                    break;
            }

            string unsupportedMessage = $"The direct method [{methodRequest.MethodName}] is not supported.";
            _logger.Trace(unsupportedMessage, TraceSeverity.Warning);
            return Task.FromResult(new DirectMethodResponse(400) { Payload = unsupportedMessage });
        }

        private async Task DesiredPropertyUpdateCallbackAsync(DesiredProperties properties)
        {
            var reported = new ReportedProperties();
            foreach (KeyValuePair<string, object> prop in properties)
            {
                // Assume all values are strings.
                if (!properties.TryGetValue(prop.Key, out string propertyValue))
                {
                    _logger.Trace($"Got request for {prop.Key} with non-string [{prop.Value}]", TraceSeverity.Warning);
                    continue;
                }

                _logger.Trace($"Got request for {prop.Key} with [{propertyValue}]", TraceSeverity.Information);
                reported.Add(prop.Key, propertyValue);
            }

            if (reported.Any())
            {
                long version = await _deviceClient.UpdateReportedPropertiesAsync(reported).ConfigureAwait(false);
                _logger.Trace($"Updated {reported.Count()} properties and observed new version {version}.", TraceSeverity.Information);

                _totalDesiredPropertiesHandled += reported.Count();
                _logger.Metric(TotalDesiredPropertiesHandled, _totalDesiredPropertiesHandled);
            }

            ++_totalTwinCallbacksHandled;
            _logger.Metric(TotalTwinCallbacksHandled, _totalTwinCallbacksHandled);
        }

        private Task<MessageAcknowledgement> OnC2dMessageReceivedAsync(IncomingMessage receivedMessage)
        {
            _logger.Trace($"Received the C2D message with Id {receivedMessage.MessageId}", TraceSeverity.Information);

            if (receivedMessage.TryGetPayload(out CustomC2dMessagePayload customC2dMessagePayload))
            {
                _logger.Metric(TotalC2dMessagesCompleted, ++_totalC2dMessagesCompleted);

                TimeSpan delay = DateTimeOffset.UtcNow - customC2dMessagePayload.SentOnUtc;
                _logger.Metric(C2dMessageOperationSeconds, delay.TotalSeconds);

                return Task.FromResult(MessageAcknowledgement.Complete);
            }

            _logger.Trace("The message payload is received in an unknown type.", TraceSeverity.Verbose);
            _logger.Metric(TotalC2dMessagesRejected, ++_totalC2dMessagesRejected);

            return Task.FromResult(MessageAcknowledgement.Reject);
        }
    }
}
