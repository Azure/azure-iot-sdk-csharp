using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.IoT.Thief.Device.LoggingConstants;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal class IotHub : IIotHub, IAsyncDisposable
    {
        private readonly string _deviceConnectionString;
        private readonly IotHubClientTransportSettings _transportSettings;
        private readonly Logger _logger;

        private SemaphoreSlim _lifetimeControl = new SemaphoreSlim(1, 1);

        private volatile bool _isConnected;
        private volatile ConnectionStatus _connectionStatus;
        private volatile int _connectionStatusChangeCount = 0;
        private readonly Stopwatch _disconnectedTimer = new Stopwatch();
        private ConnectionStatus _disconnectedStatus;
        private ConnectionStatusChangeReason _disconnectedReason;
        private volatile IotHubDeviceClient _deviceClient;

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(10);
        private readonly ConcurrentQueue<TelemetryMessage> _messagesToSend = new ConcurrentQueue<TelemetryMessage>();
        private long _totalMessagesSent = 0;

        public IDictionary<string, string> IotProperties { get; } = new Dictionary<string, string>();

        public IotHub(Logger logger, string deviceConnectionString, IotHubClientTransportSettings transportSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceConnectionString = deviceConnectionString;
            _transportSettings = transportSettings;
            _deviceClient = null;
        }

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
                    _deviceClient = new IotHubDeviceClient(_deviceConnectionString, new IotHubClientOptions(_transportSettings));
                    _deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangesHandler;
                }
                else
                {
                    await _deviceClient.CloseAsync().ConfigureAwait(false);
                }
                await _deviceClient.OpenAsync().ConfigureAwait(false);
            }
            finally
            {
                _lifetimeControl.Release();
            }
        }

        /// <summary>
        /// Runs a background
        /// </summary>
        /// <param name="ct">The cancellation token</param>
        public async Task RunAsync(CancellationToken ct)
        {
            TelemetryMessage pendingMessage = null;

            while (!ct.IsCancellationRequested)
            {
                // Wait when there are no messages to send, or if not connected
                if (!_isConnected
                    || !_messagesToSend.Any())
                {
                    try
                    {
                        await Task.Delay(s_messageLoopSleepTime, ct).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // App is signalled to exit
                        _logger.Trace($"Exit signal encountered. Terminating telemetry message pump.");
                        return;
                    }
                }

                _logger.Metric(MessageBacklog, _messagesToSend.Count);

                // If not connected, skip the work below this round
                if (!_isConnected)
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

        public void AddTelemetry(
            TelemetryBase telemetryObject,
            IDictionary<string, string> extraProperties = null)
        {
            Debug.Assert(_deviceClient != null);
            Debug.Assert(telemetryObject != null);

            // Save off the event time, or use "now" if not specified
            var createdOnUtc = telemetryObject.EventDateTimeUtc ?? DateTime.UtcNow;
            // Remove it so it does not get serialized in the message
            telemetryObject.EventDateTimeUtc = null;

            var iotMessage = new TelemetryMessage(telemetryObject)
            {
                MessageId = Guid.NewGuid().ToString(),
                // Add the event time to the system property
                CreatedOnUtc = createdOnUtc,
            };

            foreach (var prop in IotProperties)
            {
                iotMessage.Properties.TryAdd(prop.Key, prop.Value);
            }

            if (extraProperties != null)
            {
                foreach (var prop in extraProperties)
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
                .UpdateReportedPropertiesAsync(
                    reportedProperties,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace("Disposing");

            if (_lifetimeControl != null)
            {
                _lifetimeControl.Dispose();
                _lifetimeControl = null;
            }

            await _deviceClient.DisposeAsync().ConfigureAwait(false);

            _logger.Trace($"IotHub instance disposed");

        }

        private async void ConnectionStatusChangesHandler(ConnectionStatusInfo connectionInfo)
        {
            ConnectionStatus status = connectionInfo.Status;
            ConnectionStatusChangeReason reason = connectionInfo.ChangeReason;
            _logger.Trace($"Connection status changed ({++_connectionStatusChangeCount}): status=[{status}], reason=[{reason}]", TraceSeverity.Information);

            _connectionStatus = status;
            _isConnected = status == ConnectionStatus.Connected;

            if (_isConnected)
            {
                // The DeviceClient has connected.
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
                            { ConnectionStatusChangeCount, _connectionStatusChangeCount.ToString() },
                        });
                }
            }
            else if (!_isConnected && !_disconnectedTimer.IsRunning)
            {
                _disconnectedTimer.Restart();
                _disconnectedStatus = status;
                _disconnectedReason = reason;
            }

            switch (connectionInfo.RecommendedAction)
            {
                case RecommendedAction.OpenConnection:
                    _logger.Trace($"Following recommended action of reinitializing the client.", TraceSeverity.Information);
                    await InitializeAsync();
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
    }
}
