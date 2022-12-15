// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class DeviceReconnectionSample
    {
        private static readonly Random s_randomGenerator = new();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(15);
        private static readonly SemaphoreSlim s_initSemaphore = new(1, 1);

        private readonly List<string> _deviceConnectionStrings;
        private readonly IotHubClientOptions _clientOptions;
        private readonly IIotHubClientRetryPolicy _customRetryPolicy;

        private readonly string _certificatePath;
        private readonly string _certificatePassword;
        private readonly string _deviceId;
        private readonly string _hostname;

        // An UnauthorizedException is handled in the connection status change handler through its corresponding status change event.
        // We will ignore this exception when thrown by client API operations.
        private readonly HashSet<Type> _exceptionsToBeRetried = new()
        {
            // Unauthorized exception conditions are handled by the ConnectionStatusChangeHandler in this sample and the sample will try
            // to reconnect indefinitely, so don't give up on an operation that sees this exception.
            typeof(IotHubClientException),
        };

        private readonly ILogger _logger;

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile IotHubDeviceClient s_deviceClient;

        private static CancellationTokenSource s_appCancellation;

        // A safe initial value for caching the twin desired properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localDesiredPropertyVersion = 1;

        public DeviceReconnectionSample(List<string> deviceConnectionStrings, Parameters parameters, ILogger logger)
        {
            _logger = logger;
            _customRetryPolicy = new CustomRetryPolicy(_exceptionsToBeRetried, logger);

            // This class takes a list of potentially valid connection strings (most likely the currently known good primary and secondary keys)
            // and will attempt to connect with the first. If it receives feedback that a connection string is invalid, it will discard it, and
            // if any more are remaining, will try the next one.
            // To test this, either pass an invalid connection string as the first one, or rotate it while the sample is running, and wait about
            // 5 minutes.
            if (deviceConnectionStrings == null
                || !deviceConnectionStrings.Any())
            {
                throw new ArgumentException("At least one connection string must be provided.", nameof(deviceConnectionStrings));
            }
            _deviceConnectionStrings = deviceConnectionStrings;
            _logger.LogInformation($"Supplied with {_deviceConnectionStrings.Count} connection string(s).");

            _clientOptions = new(parameters.GetHubTransportSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                RetryPolicy = _customRetryPolicy,
            };

            if (!String.IsNullOrWhiteSpace(parameters.DeviceId)){
                _deviceId = parameters.DeviceId;
            }

            if (!String.IsNullOrWhiteSpace(parameters.CertificateName)){
                _certificatePath = parameters.CertificateName;
            }

            if (!String.IsNullOrWhiteSpace(parameters.CertificatePassword)){
                _certificatePassword = parameters.CertificatePassword;
            }

            if (!String.IsNullOrWhiteSpace(parameters.HostName)){
                _hostname = parameters.HostName;
            }
        }

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            s_appCancellation = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_appCancellation.Cancel();
                _logger.LogWarning("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation($"Sample execution started, press Control+C to quit the sample.");

            try
            {
                await InitializeAndSetupClientAsync(s_appCancellation.Token);
                await SendMessagesAsync(s_appCancellation.Token);
            }
            catch (OperationCanceledException) { } // User canceled the operation
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting: \n{ex}");
                s_appCancellation.Cancel();
            }

            await s_deviceClient.DisposeAsync();
            s_initSemaphore.Dispose();
            s_appCancellation.Dispose();
        }

        private async Task InitializeAndSetupClientAsync(CancellationToken cancellationToken)
        {
            if (ShouldClientBeInitialized())
            {
                // Allow a single thread to close and re-open the client instance.
                await s_initSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (ShouldClientBeInitialized())
                    {
                        _logger.LogDebug($"Attempting to initialize the client instance, current status={s_deviceClient?.ConnectionStatusInfo.Status}");

                        // If the device client instance has been previously initialized, close it.
                        if (s_deviceClient != null)
                        {
                            try
                            {
                                await s_deviceClient.CloseAsync(cancellationToken);
                            }
                            catch (IotHubClientException) { } // if the previous token is now invalid, this call may fail
                        }
                        else
                        {
                            // Otherwise instantiate it for the first time.
                            s_deviceClient = new IotHubDeviceClient(_deviceConnectionStrings.First(), _clientOptions);
                        }

                        s_deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandlerAsync;
                        _logger.LogDebug("Initialized the client instance.");

                        // Force connection now.
                        // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                        await s_deviceClient.OpenAsync(cancellationToken);
                        _logger.LogDebug($"The client instance has been opened.");
                    }
                }
                finally
                {
                    s_initSemaphore.Release();
                }

                // You will need to resubscribe to any client callbacks any time the client is initialized.
                await s_deviceClient.SetIncomingMessageCallbackAsync(OnMessageReceivedAsync, cancellationToken);
                _logger.LogDebug("The client has subscribed to cloud-to-device messages.");

                await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, cancellationToken);
                _logger.LogDebug("The client has subscribed to desired property update notifications.");
            }
        }

        // It is not generally a good practice to have async void methods, however, IotHubDeviceClient.ConnectionStatusChangeHandlerAsync() event handler signature
        // has a void return type. As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock before attempting to
        // initialize or close the device client instance; the async method GetTwinAndDetectChangesAsync is implemented similarly for the same purpose.
        private async void ConnectionStatusChangeHandlerAsync(ConnectionStatusInfo connectionInfo)
        {
            ConnectionStatus status = connectionInfo.Status;
            ConnectionStatusChangeReason reason = connectionInfo.ChangeReason;
            Console.WriteLine($"Connection status changed: status={status}, reason={reason}, recommendation={connectionInfo.RecommendedAction}");

            // In our case, we can operate with more than 1 shared access key and attempt to fall back to a secondary.
            // We'll disregard the SDK's recommendation and attempt to connect with the second one.
            if (status == ConnectionStatus.Disconnected
                && reason == ConnectionStatusChangeReason.BadCredential
                && _deviceConnectionStrings.Count > 1)
            {
                // When getting this reason, the current connection string being used is not valid.
                // If we had a backup, we can try using that.
                _deviceConnectionStrings.RemoveAt(0);
                Console.WriteLine($"The current connection string is invalid. Trying another.");
                await InitializeAndSetupClientAsync(s_appCancellation.Token);
                return;
            }

            // Otherwise, we follow the SDK's recommendation.
            switch (connectionInfo.RecommendedAction)
            {
                case RecommendedAction.OpenConnection:
                    Console.WriteLine($"Following recommended action of reinitializing the client.");
                    await InitializeAndSetupClientAsync(s_appCancellation.Token);
                    break;

                case RecommendedAction.PerformNormally:
                    // Call GetTwinAndDetectChangesAsync() to retrieve twin values from the server once the connection status changes into Connected.
                    // This can get back "lost" twin updates in a device reconnection from status like Disconnected_Retrying or Disconnected.
                    await GetTwinAndDetectChangesAsync(s_appCancellation.Token);
                    Console.WriteLine("The client has retrieved twin values after the connection status changes into CONNECTED.");
                    break;

                case RecommendedAction.WaitForRetryPolicy:
                    Console.WriteLine("Letting the client retry.");
                    break;

                case RecommendedAction.Quit:
                    s_appCancellation.Cancel();
                    break;
            }
        }

        private async Task GetTwinAndDetectChangesAsync(CancellationToken cancellationToken)
        {
            TwinProperties twin = await s_deviceClient.GetTwinPropertiesAsync(s_appCancellation.Token);
            _logger.LogInformation($"Device retrieving twin values: {twin.Desired.GetSerializedString()}");

            DesiredProperties desiredProperties = twin.Desired;
            long serverDesiredPropertyVersion = desiredProperties.Version;

            // Check if the desired property version is outdated on the local side.
            if (serverDesiredPropertyVersion > s_localDesiredPropertyVersion)
            {
                _logger.LogDebug($"The desired property version cached on local is changing from {s_localDesiredPropertyVersion} to {serverDesiredPropertyVersion}.");
                await HandleTwinUpdateNotificationsAsync(desiredProperties);
            }
        }

        private async Task HandleTwinUpdateNotificationsAsync(DesiredProperties twinUpdateRequest)
        {
            CancellationToken cancellationToken = s_appCancellation.Token;

            if (!cancellationToken.IsCancellationRequested)
            {
                var reportedProperties = new ReportedProperties();

                _logger.LogInformation($"Twin property update requested: \n{twinUpdateRequest.GetSerializedString()}");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                foreach (KeyValuePair<string, object> desiredProperty in twinUpdateRequest)
                {
                    _logger.LogInformation($"Setting property {desiredProperty.Key} to {desiredProperty.Value}.");
                    reportedProperties[desiredProperty.Key] = desiredProperty.Value;
                }

                s_localDesiredPropertyVersion = twinUpdateRequest.Version;
                _logger.LogDebug($"The desired property version on local is currently {s_localDesiredPropertyVersion}.");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            }
        }

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (s_deviceClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected)
                {
                    _logger.LogInformation($"Device sending message {++messageCount} to IoT hub.");

                    TelemetryMessage message = PrepareMessage(messageCount);
                    await s_deviceClient.SendTelemetryAsync(message, cancellationToken);
                    _logger.LogInformation($"Device sent message {messageCount} to IoT hub.");
                }

                await Task.Delay(s_sleepDuration, cancellationToken);
            }
        }

        private Task<MessageAcknowledgement> OnMessageReceivedAsync(IncomingMessage receivedMessage)
        {
            bool messageDeserialized = receivedMessage.TryGetPayload(out string messageData);
            if (messageDeserialized)
            {
                var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

                foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
                {
                    formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
                }
                _logger.LogInformation(formattedMessage.ToString());

                _logger.LogInformation($"Completed message [{messageData}].");
                return Task.FromResult(MessageAcknowledgement.Complete);
            }

            // A message was received that did not conform to the serialization specifications; ignore it.
            return Task.FromResult(MessageAcknowledgement.Reject);
        }

        private static TelemetryMessage PrepareMessage(int messageId)
        {
            const int temperatureThreshold = 30;

            int temperature = s_randomGenerator.Next(20, 35);
            int humidity = s_randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new TelemetryMessage(messagePayload)
            {
                MessageId = messageId.ToString(),
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > temperatureThreshold) ? "true" : "false");

            return eventMessage;
        }

        private bool ShouldClientBeInitialized()
        {
            return s_deviceClient?.ConnectionStatusInfo == null
                || s_deviceClient.ConnectionStatusInfo.RecommendedAction == RecommendedAction.OpenConnection
                && _deviceConnectionStrings.Any();
        }
    }
}
