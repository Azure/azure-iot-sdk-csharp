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
    internal sealed class IotHub : IIotHub, IAsyncDisposable
    {
        private readonly string _deviceConnectionString;
        private readonly IotHubClientTransportSettings _transportSettings;
        private readonly Logger _logger;

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        private volatile int _connectionStatusChangeCount = 0;
        private readonly Stopwatch _disconnectedTimer = new();
        private ConnectionStatus _disconnectedStatus;
        private ConnectionStatusChangeReason _disconnectedReason;
        private RecommendedAction _disconnectedRecommendedAction;
        private volatile IotHubDeviceClient _deviceClient;

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_deviceTwinUpdateInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_fileUploadInterval = TimeSpan.FromSeconds(5);
        private readonly ConcurrentQueue<TelemetryMessage> _messagesToSend = new();

        private long _totalTelemetryMessagesSent = 0;
        private long _totalTwinUpdatesReported = 0;
        private long _totalTwinCallbacksHandled = 0;
        private long _totalDesiredPropertiesHandled = 0;
        private long _totalC2dMessagesCompleted = 0;
        private long _totalC2dMessagesRejected = 0;

        public Dictionary<string, string> TelemetryUserProperties { get; } = new();

        public IotHub(Logger logger, string deviceConnectionString, IotHubClientTransportSettings transportSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceConnectionString = deviceConnectionString;
            _transportSettings = transportSettings;
            _deviceClient = null;
        }

        public bool IsConnected => _deviceClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected;

        /// <summary>
        /// Initializes the connection to IoT Hub.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _lifetimeControl.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_deviceClient == null)
                {
                    _deviceClient = new IotHubDeviceClient(
                        _deviceConnectionString,
                        new IotHubClientOptions(_transportSettings)
                        {
                            PayloadConvention = SystemTextJsonPayloadConvention.Instance,
                        })
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
            TelemetryMessage pendingMessage = null;
            bool loggedDisconnection = false;
            logger.LoggerContext.Add(OperationName, LoggingConstants.TelemetryMessage);
            while (!ct.IsCancellationRequested)
            {
                logger.Metric(MessageBacklog, _messagesToSend.Count);

                // Wait when there are no messages to send, or if not connected
                if (!IsConnected
                    || !_messagesToSend.Any())
                {
                    try
                    {
                        await Task.Delay(s_messageLoopSleepTime, ct).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // App is signalled to exit
                        _logger.Trace($"Exit signal encountered. Terminating telemetry message pump.", TraceSeverity.Verbose);
                        return;
                    }
                }

                // If not connected, skip the work below this round
                if (!IsConnected)
                {
                    if (!loggedDisconnection)
                    {
                        loggedDisconnection = true;
                        logger.Trace($"Waiting for connection before sending telemetry", TraceSeverity.Warning);
                    }
                    continue;
                }

                // Make get a message to send, unless we're retrying a previous message
                if (pendingMessage == null)
                {
                    _messagesToSend.TryDequeue(out pendingMessage);
                }

                // Send any message prepped to send
                if (pendingMessage != null)
                {
                    var sw = Stopwatch.StartNew();
                    await _deviceClient.SendTelemetryAsync(pendingMessage, ct).ConfigureAwait(false);
                    sw.Stop();

                    _logger.Metric(TotalTelemetryMessagesSent, ++_totalTelemetryMessagesSent);
                    _logger.Metric(TelemetryMessageDelaySeconds, sw.Elapsed.TotalSeconds);

                    pendingMessage = null;
                }
            }
        }

        public async Task ReportReadOnlyPropertiesAsync(Logger logger, CancellationToken ct)
        {
            bool loggedDisconnection = false;
            logger.LoggerContext.Add(OperationName, ReportTwinProperties);
            while (!ct.IsCancellationRequested)
            {
                // If not connected, skip the work below this round
                if (IsConnected)
                {
                    var reported = new ReportedProperties
                    {
                        { "TotalTelemetryMessagesSent", _totalTelemetryMessagesSent },
                    };
                    await _deviceClient.UpdateReportedPropertiesAsync(reported, ct).ConfigureAwait(false);

                    ++_totalTwinUpdatesReported;
                    logger.Metric(TotalTwinUpdatesReported, _totalTwinUpdatesReported);
                }
                else if (!loggedDisconnection)
                {
                    loggedDisconnection = true;
                    logger.Trace($"Waiting for connection before any other operations.", TraceSeverity.Warning);
                }

                await Task.Delay(s_deviceTwinUpdateInterval, ct).ConfigureAwait(false);
            }
        }

        public void AddTelemetry(
            TelemetryBase telemetryObject,
            IDictionary<string, string> extraProperties = null)
        {
            Debug.Assert(_deviceClient != null);
            Debug.Assert(telemetryObject != null);

            // Save off the event time, or use "now" if not specified
            DateTime createdOnUtc = telemetryObject.EventDateTimeUtc ?? DateTime.UtcNow;
            // Remove it so it does not get serialized in the message
            telemetryObject.EventDateTimeUtc = null;

            var iotMessage = new TelemetryMessage(telemetryObject)
            {
                MessageId = Guid.NewGuid().ToString(),
                // Add the event time to the system property
                CreatedOnUtc = createdOnUtc,
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

        public async Task SetPropertiesAsync(string keyName, object properties, CancellationToken cancellationToken)
        {
            Debug.Assert(_deviceClient != null);
            Debug.Assert(properties != null);

            var reportedProperties = new ReportedProperties
            {
                { keyName, properties },
            };

            await _deviceClient
                .UpdateReportedPropertiesAsync(reportedProperties, cancellationToken)
                .ConfigureAwait(false);

            _logger.Trace($"Set the reported property with name [{keyName}] in device twin.", TraceSeverity.Information);
        }

        public async Task UploadFilesAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, UploadFiles);
            while (!ct.IsCancellationRequested)
            {
                string fileName = $"TestPayload-{Guid.NewGuid()}.txt";
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes("TestPayload"));

                var fileUploadSasUriRequest = new FileUploadSasUriRequest(fileName);
                FileUploadSasUriResponse sasUri = await _deviceClient
                    .GetFileUploadSasUriAsync(fileUploadSasUriRequest, ct)
                    .ConfigureAwait(false);

                Uri uploadUri = sasUri.GetBlobUri();

                try
                {
                    logger.Trace($"Attempting to upload {fileName}...", TraceSeverity.Information);
                    var blockBlobClient = new BlockBlobClient(uploadUri);
                    await blockBlobClient.UploadAsync(ms, new BlobUploadOptions(), ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception occurred while using Azure Storage SDK to upload file: {ex}", TraceSeverity.Warning);
                    var failedFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, false)
                    {
                        StatusCode = 500,
                    };

                    await _deviceClient.CompleteFileUploadAsync(failedFileUploadCompletionNotification, ct).ConfigureAwait(false);
                    return;
                }

                logger.Trace("File uploaded to Azure Storage was a success", TraceSeverity.Information);
                var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, true)
                {
                    StatusCode = 200,
                };

                await _deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification, ct).ConfigureAwait(false);
                await Task.Delay(s_fileUploadInterval, ct).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            if (_lifetimeControl != null)
            {
                _lifetimeControl.Dispose();
                _lifetimeControl = null;
            }

            await _deviceClient.DisposeAsync().ConfigureAwait(false);

            _logger.Trace($"IoT Hub client instance disposed", TraceSeverity.Verbose);

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
                    await InitializeAsync().ConfigureAwait(false);
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
            _logger.Trace($"Received direct method [{methodRequest.MethodName}] with payload [{methodRequest.GetPayloadAsJsonString()}].", TraceSeverity.Information);

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
                                (DateTimeOffset.UtcNow - methodPayload.CurrentTimeUtc).TotalSeconds);

                            // Log the current time again and send the response back to the service app.
                            methodPayload.CurrentTimeUtc = DateTimeOffset.UtcNow;
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
                await _deviceClient.UpdateReportedPropertiesAsync(reported).ConfigureAwait(false);

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
                _logger.Trace("The message payload is received in an expected type.", TraceSeverity.Verbose);
                _logger.Metric(TotalC2dMessagesCompleted, ++_totalC2dMessagesCompleted);

                TimeSpan delay = DateTimeOffset.UtcNow - customC2dMessagePayload.CurrentTimeUtc;
                _logger.Metric(C2dMessageDelaySeconds, delay.TotalSeconds);

                return Task.FromResult(MessageAcknowledgement.Complete);
            }

            _logger.Trace("The message payload is received in an unknown type.", TraceSeverity.Verbose);
            _logger.Metric(TotalC2dMessagesRejected, ++_totalC2dMessagesRejected);

            return Task.FromResult(MessageAcknowledgement.Reject);
        }
    }
}
