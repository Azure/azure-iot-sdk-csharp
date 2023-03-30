using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;
using static Microsoft.Azure.Devices.LongHaul.Device.LoggingConstants;
using System.Text;

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
        private readonly ConcurrentQueue<TelemetryMessage> _messagesToSend = new();
        private long _totalMessagesSent = 0;
        private long _totalTwinUpdatesReported = 0;
        private long _totalTwinCallbacksHandled = 0;
        private long _totalDesiredPropertiesHandled = 0;

        public IDictionary<string, string> IotProperties { get; } = new Dictionary<string, string>();

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
                    _deviceClient = new IotHubDeviceClient(_deviceConnectionString, new IotHubClientOptions(_transportSettings))
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
        public async Task SendTelemetryMessagesAsync(CancellationToken ct)
        {
            TelemetryMessage pendingMessage = null;

            while (!ct.IsCancellationRequested)
            {
                _logger.Metric(MessageBacklog, _messagesToSend.Count);

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
                    _logger.Trace($"Waiting for connection before sending telemetry", TraceSeverity.Warning);
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
                    await _deviceClient.SendTelemetryAsync(pendingMessage, ct).ConfigureAwait(false);

                    ++_totalMessagesSent;
                    _logger.Metric(TotalMessagesSent, _totalMessagesSent);
                    _logger.Metric(MessageDelaySeconds, (DateTime.UtcNow - pendingMessage.CreatedOnUtc).TotalSeconds);

                    pendingMessage = null;
                }
            }
        }

        public async Task ReportReadOnlyPropertiesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // If not connected, skip the work below this round
                if (IsConnected)
                {
                    var reported = new ReportedProperties
                    {
                        { "TotalMessagesSent", _totalMessagesSent },
                    };
                    await _deviceClient.UpdateReportedPropertiesAsync(reported ,ct).ConfigureAwait(false);

                    ++_totalTwinUpdatesReported;
                    _logger.Metric(TotalTwinUpdatesReported, _totalTwinUpdatesReported);
                }
                else
                {
                    _logger.Trace($"Waiting for connection before any other operations.", TraceSeverity.Warning);
                    continue;
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

            foreach (KeyValuePair<string, string> prop in IotProperties)
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

        public async Task UploadFileAsync(CancellationToken ct)
        {
            string fileName = $"TestPayload-{Guid.NewGuid()}.txt";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("TestPayload"));
            _logger.Trace($"Uploading file {fileName}");

            var fileUploadTime = Stopwatch.StartNew();

            var fileUploadSasUriRequest = new FileUploadSasUriRequest(fileName);
            FileUploadSasUriResponse sasUri = await _deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest).ConfigureAwait(false);
            Uri uploadUri = sasUri.GetBlobUri();

            try
            {
                _logger.Trace($"Attempting to upload {fileName}...");
                var blockBlobClient = new BlockBlobClient(uploadUri);
                await blockBlobClient.UploadAsync(ms, new BlobUploadOptions());
            }
            catch (Exception ex)
            {
                _logger.Trace($"WARNING: Exception occured while using Azure Storage SDK to upload file: {ex.Message}");

                var failedFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, false);
                await _deviceClient.CompleteFileUploadAsync(failedFileUploadCompletionNotification);
                return;
            }

            _logger.Trace("File upload to Azure Storage was a success");
            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification(sasUri.CorrelationId, true);
            await _deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification, ct);
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
    }
}
