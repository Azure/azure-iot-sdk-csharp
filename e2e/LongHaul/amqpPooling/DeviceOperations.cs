// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.AmqpPooling.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class DeviceOperations : IAsyncDisposable
    {
        private readonly IotHubDeviceClient _deviceClient;
        private readonly string _deviceId;
        private readonly Logger _logger;
        private readonly Stopwatch _disconnectedTimer = new();

        private ConnectionStatus _disconnectedStatus;
        private ConnectionStatusChangeReason _disconnectedReason;
        private RecommendedAction _disconnectedRecommendedAction;

        private volatile int _connectionStatusChangeCount = 0;
        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);

        public DeviceOperations(IotHubDeviceClient deviceClient, string deviceId, Logger logger)
        {
            _deviceClient = deviceClient;
            _deviceId = deviceId;
            logger.LoggerContext.Add("deviceId", deviceId);
            _logger = logger;
        }

        public bool IsConnected => _deviceClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected;

        public async Task InitializeAsync()
        {
            // Set the device client connection status change handler
            _deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangesHandlerAsync;

            await _deviceClient.OpenAsync().ConfigureAwait(false);
        }

        public async Task SendMessagesAsync(CancellationToken ct)
        {
            var sw = new Stopwatch();
            long totalTelemetryMessagesSent = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var message = new TelemetryMessage(
                        new DeviceTelemetry
                        {
                            DeviceId = _deviceId,
                            GuidValue = Guid.NewGuid().ToString(),
                        });


                    sw.Restart();
                    await _deviceClient.SendTelemetryAsync(message, ct).ConfigureAwait(false);
                    sw.Stop();
                    _logger.Trace($"Sent a telemetry message from the device with Id [{_deviceId}].", TraceSeverity.Information);

                    _logger.Metric(TelemetryMessageDelaySeconds, sw.Elapsed.TotalSeconds);
                    _logger.Metric(TotalTelemetryMessagesSent, ++totalTelemetryMessagesSent);
                }
                catch (Exception ex)
                {
                    _logger.Trace($"Exception when sending telemetry from the device with Id [{_deviceId}].\n{ex}", TraceSeverity.Warning);
                }

                await Task.Delay(s_messageLoopSleepTime).ConfigureAwait(false);
            }
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
                $"Device [{_deviceId}]: Connection status changed ({++_connectionStatusChangeCount}).\n" +
                $"status=[{status}], reason=[{reason}], recommendation=[{recommendedAction}]",
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

        public async ValueTask DisposeAsync()
        {
            await _deviceClient.DisposeAsync();
        }

        private class DeviceTelemetry
        {
            [JsonProperty("deviceId")]
            public string DeviceId { get; set; }

            [JsonProperty("guidValue")]
            public string GuidValue { get; set; }

            [JsonProperty("sentTimeUtc")]
            public DateTimeOffset SentOnUtc { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
