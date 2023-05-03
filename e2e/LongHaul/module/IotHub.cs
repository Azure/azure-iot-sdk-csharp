// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.LongHaul.Module.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Module
{
    internal sealed class IotHub : IAsyncDisposable
    {
        private readonly string _moduleConnectionString;
        private readonly IotHubClientOptions _clientOptions;
        private readonly Logger _logger;
        private CancellationToken _ct;

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        private volatile int _connectionStatusChangeCount;
        private readonly Stopwatch _disconnectedTimer = new();
        private ConnectionStatus _disconnectedStatus;
        private ConnectionStatusChangeReason _disconnectedReason;
        private RecommendedAction _disconnectedRecommendedAction;
        private volatile IotHubModuleClient _moduleClient;

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_deviceTwinUpdateInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(1);
        private readonly ConcurrentQueue<TelemetryMessage> _messagesToSend = new();

        private long _totalTelemetryMessagesToModuleSent;
        private long _totalTwinUpdatesToModuleReported;
        private long _totalTwinCallbacksToModuleHandled;
        private long _totalDesiredPropertiesToModuleHandled;
        private long _totalM2mMessagesCompleted;
        private long _totalM2mMessagesRejected;
        private long _totalMethodCallsServiceToModuleCount;
        private long _totalMethodCallsModuleToModuleSentCount;
        private long _totalMethodCallsModuleToModuleReceivedCount;

        public IotHub(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _moduleConnectionString = parameters.GatewayHostName == null ? parameters.DeviceModuleConnectionString : parameters.EdgeModuleConnectionString;
            _clientOptions = new IotHubClientOptions(parameters.GetTransportSettings())
            {
                PayloadConvention = parameters.GetPayloadConvention(),
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                GatewayHostName = parameters.GatewayHostName,
            };
            _moduleClient = null;
        }

        public bool IsConnected => _moduleClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected;

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
            var sw = new Stopwatch();

            try
            {
                if (_moduleClient == null)
                {
                    _moduleClient = new IotHubModuleClient(_moduleConnectionString, _clientOptions)
                    {
                        ConnectionStatusChangeCallback = ConnectionStatusChangesHandlerAsync
                    };
                }
                else
                {
                    sw.Restart();
                    await _moduleClient.CloseAsync().ConfigureAwait(false);
                    sw.Stop();
                    _logger.Metric(ModuleClientCloseDelaySeconds, sw.Elapsed.TotalSeconds);
                }

                sw.Restart();
                await _moduleClient.OpenAsync().ConfigureAwait(false);
                sw.Stop();
                _logger.Metric(ModuleClientOpenDelaySeconds, sw.Elapsed.TotalSeconds);
                await _moduleClient.SetDirectMethodCallbackAsync(DirectMethodCallback).ConfigureAwait(false);
                await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallbackAsync).ConfigureAwait(false);
                await _moduleClient.SetIncomingMessageCallbackAsync(OnIncomingMessageReceivedAsync).ConfigureAwait(false);
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
        public async Task SendMessagesToRouteAsync(Logger logger, CancellationToken ct)
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
                logger.Metric(ModuleMessageToRouteBacklog, _messagesToSend.Count);

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

                if (pendingMessages.Any())
                {
                    try
                    {
                        if (pendingMessages.Count > 1)
                        {
                            logger.Trace($"Sending {pendingMessages.Count} messages in bulk.");
                            sw.Restart();
                            await _moduleClient.SendMessagesToRouteAsync("*", pendingMessages, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            sw.Restart();
                            await _moduleClient.SendMessageToRouteAsync("*", pendingMessages.First(), ct).ConfigureAwait(false);
                        }
                        sw.Stop();

                        _totalTelemetryMessagesToModuleSent += pendingMessages.Count;
                        _logger.Metric(TotalTelemetryMessagesToModuleSent, _totalTelemetryMessagesToModuleSent);
                        _logger.Metric(TelemetryMessageToModuleDelaySeconds, sw.Elapsed.TotalSeconds);
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

        public async Task ReportReadOnlyPropertiesAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, ReportTwinProperties);
            var sw = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                sw.Restart();
                await SetPropertiesAsync("totalTelemetryMessagesSent", _totalTelemetryMessagesToModuleSent, logger, ct).ConfigureAwait(false);
                sw.Stop();

                ++_totalTwinUpdatesToModuleReported;
                logger.Metric(TotalTwinUpdatesToModuleReported, _totalTwinUpdatesToModuleReported);
                logger.Metric(ReportedTwinUpdateToModuleOperationSeconds, sw.Elapsed.TotalSeconds);

                await Task.Delay(s_deviceTwinUpdateInterval, ct).ConfigureAwait(false);
            }
        }

        public void AddTelemetry(
            TelemetryBase telemetryObject,
            IDictionary<string, string> extraProperties = null)
        {
            Debug.Assert(_moduleClient != null);
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

        public async Task SetPropertiesAsync(string keyName, object properties, Logger logger, CancellationToken ct)
        {
            Debug.Assert(_moduleClient != null);
            Debug.Assert(properties != null);

            var reportedProperties = new ReportedProperties
            {
                { keyName, properties },
            };

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    long version = await _moduleClient
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
        }

        public async Task InvokeDirectMethodOnLeafClientThroughEdgeAsync(string deviceId, string moduleId, string methodName, Logger logger, CancellationToken ct)
        {
            Debug.Assert(_moduleClient != null);
            Debug.Assert(deviceId != null);
            Debug.Assert(moduleId != null);

            var sw = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var payload = new CustomDirectMethodPayload
                    {
                        RandomId = Guid.NewGuid(),
                        CurrentTimeUtc = DateTimeOffset.UtcNow,
                    };

                    var directMethodRequest = new EdgeModuleDirectMethodRequest(methodName, payload)
                    {
                        ResponseTimeout = TimeSpan.FromSeconds(30),
                    };

                    logger.Trace($"Invoking direct method for edge device: {deviceId}, edge module: {moduleId}", TraceSeverity.Information);
                    sw.Restart();

                    // Invoke the direct method asynchronously and get the response from the simulated module.
                    DirectMethodResponse response = await _moduleClient
                        .InvokeMethodAsync(deviceId, moduleId, directMethodRequest, ct)
                        .ConfigureAwait(false);

                    logger.Metric(TotalDirectMethodCallsModuleToModuleSentCount, ++_totalMethodCallsModuleToModuleSentCount);
                    logger.Metric(DirectMethodModuleToModuleRoundTripSeconds, sw.Elapsed.TotalSeconds);

                    if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                    {
                        logger.Metric(
                            DirectMethodModuleToModuleDelaySeconds,
                            (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                    }

                    logger.Trace($"Response status: {response.Status}, method name:{methodName}", TraceSeverity.Information);
                    break;
                }
                catch (Exception ex)
                {
                    logger.Trace($"Exception invoking direct method from an edge module\n{ex}", TraceSeverity.Warning);
                    await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
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

            await _moduleClient.DisposeAsync().ConfigureAwait(false);

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
                        ModuleDisconnectedDurationSeconds,
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
                                C2mDirectMethodDelaySeconds,
                                (DateTimeOffset.UtcNow - methodPayload.CurrentTimeUtc).TotalSeconds);
                            _logger.Metric(TotalDirectMethodCallsServiceToModuleCount, ++_totalMethodCallsServiceToModuleCount);

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

                case "ModuleToItself":
                    try
                    {
                        if (methodRequest.TryGetPayload(out CustomDirectMethodPayload methodPayload))
                        {
                            _logger.Trace($"Echoing back the payload of direct method.", TraceSeverity.Information);
                            _logger.Metric(
                                C2mDirectMethodDelaySeconds,
                                (DateTimeOffset.UtcNow - methodPayload.CurrentTimeUtc).TotalSeconds);
                            _logger.Metric(TotalDirectMethodCallsModuleToModuleReceivedCount, ++_totalMethodCallsModuleToModuleReceivedCount);

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
                long version = await _moduleClient.UpdateReportedPropertiesAsync(reported).ConfigureAwait(false);
                _logger.Trace($"Updated {reported.Count()} properties and observed new version {version}.", TraceSeverity.Information);

                _totalDesiredPropertiesToModuleHandled += reported.Count();
                _logger.Metric(TotalDesiredPropertiesToModuleHandled, _totalDesiredPropertiesToModuleHandled);
            }

            ++_totalTwinCallbacksToModuleHandled;
            _logger.Metric(TotalTwinCallbacksToModuleHandled, _totalTwinCallbacksToModuleHandled);
        }

        private Task<MessageAcknowledgement> OnIncomingMessageReceivedAsync(IncomingMessage receivedMessage)
        {
            _logger.Trace($"Received the M2M message with Id {receivedMessage.MessageId}", TraceSeverity.Information);

            if (receivedMessage.TryGetPayload(out CustomC2mMessagePayload customC2mMessagePayload))
            {
                _logger.Metric(TotalM2mMessagesCompleted, ++_totalM2mMessagesCompleted);

                TimeSpan delay = DateTimeOffset.UtcNow - customC2mMessagePayload.CurrentTimeUtc;
                _logger.Metric(M2mMessageOperationSeconds, delay.TotalSeconds);

                return Task.FromResult(MessageAcknowledgement.Complete);
            }

            _logger.Trace("The message payload is received in an unknown type.", TraceSeverity.Verbose);
            _logger.Metric(TotalM2mMessagesRejected, ++_totalM2mMessagesRejected);

            return Task.FromResult(MessageAcknowledgement.Reject);
        }
    }
}
