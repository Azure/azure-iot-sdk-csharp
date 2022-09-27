// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class DeviceReconnectionSample
    {
        private const int TemperatureThreshold = 30;
        private static readonly Random s_randomGenerator = new();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(15);

        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly List<string> _deviceConnectionStrings;
        private readonly IotHubClientOptions _clientOptions;

        // An UnauthorizedException is handled in the connection status change handler through its corresponding status change event.
        // We will ignore this exception when thrown by client API operations.
        private readonly Dictionary<Type, string> _exceptionsToBeIgnored = new()
        {
            { typeof(IotHubClientException), "Unauthorized exceptions are handled by the ConnectionStatusChangeHandler." }
        };

        private readonly ILogger _logger;

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile IotHubDeviceClient s_deviceClient;

        private static volatile ConnectionStatus s_connectionStatus = ConnectionStatus.Disconnected;

        private static CancellationTokenSource s_cancellationTokenSource;

        // A safe initial value for caching the twin desired properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localDesiredPropertyVersion = 1;

        public DeviceReconnectionSample(List<string> deviceConnectionStrings, Parameters parameters, ILogger logger)
        {
            _logger = logger;

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

            _clientOptions = new(parameters.GetHubTransportSettings()) { SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset };
        }

        private static bool IsDeviceConnected => s_connectionStatus == ConnectionStatus.Connected;

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            s_cancellationTokenSource = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_cancellationTokenSource.Cancel();
                _logger.LogWarning("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation($"Sample execution started, press Control+C to quit the sample.");

            try
            {
                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                await SendMessagesAsync(s_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { } // User canceled the operation
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting: \n{ex}");
                s_cancellationTokenSource.Cancel();
            }

            _initSemaphore.Dispose();
            s_cancellationTokenSource.Dispose();
        }

        private async Task InitializeAndSetupClientAsync(CancellationToken cancellationToken)
        {
            if (ShouldClientBeInitialized(s_connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                await _initSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (ShouldClientBeInitialized(s_connectionStatus))
                    {
                        _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                        // If the device client instance has been previously initialized, close and dispose it.
                        if (s_deviceClient != null)
                        {
                            try
                            {
                                await s_deviceClient.CloseAsync(cancellationToken);
                            }
                            catch (IotHubClientException) { } // if the previous token is now invalid, this call may fail
                            s_deviceClient.Dispose();
                        }

                        s_deviceClient = new IotHubDeviceClient(_deviceConnectionStrings.First(), _clientOptions);
                        s_deviceClient.SetConnectionStatusChangeHandler(ConnectionStatusChangeHandlerAsync);
                        await s_deviceClient.SetReceiveMessageHandlerAsync(OnMessageReceivedAsync, cancellationToken);
                        _logger.LogDebug("Initialized the client instance.");
                    }
                }
                finally
                {
                    _initSemaphore.Release();
                }

                // Force connection now.
                // We have set the "shouldExecuteOperation" function to always try to open the connection.
                // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "OpenConnection",
                    asyncOperation: async () => await s_deviceClient.OpenAsync(cancellationToken),
                    shouldExecuteOperation: () => true,
                    logger: _logger,
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
                _logger.LogDebug($"The client instance has been opened.");

                // You will need to subscribe to the client callbacks any time the client is initialized.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "SubscribeTwinUpdates",
                    asyncOperation: async () => await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, cancellationToken),
                    shouldExecuteOperation: () => IsDeviceConnected,
                    logger: _logger,
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
                _logger.LogDebug("The client has subscribed to desired property update notifications.");
            }
        }

        // It is not generally a good practice to have async void methods, however, IotHubDeviceClient.ConnectionStatusChangeHandlerAsync() event handler signature
        // has a void return type. As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock before attempting to
        // initialize or dispose the device client instance; the async method GetTwinAndDetectChangesAsync is implemented similarly for the same purpose.
        private async void ConnectionStatusChangeHandlerAsync(ConnectionStatusInfo connectionInfo)
        {
            _logger.LogDebug($"Connection status changed: status={connectionInfo.Status}, reason={connectionInfo.ChangeReason}");
            s_connectionStatus = connectionInfo.Status;

            switch (connectionInfo.Status)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    // Call GetTwinAndDetectChangesAsync() to retrieve twin values from the server once the connection status changes into Connected.
                    // This can get back "lost" twin updates in a device reconnection from status like Disconnected_Retrying or Disconnected.
                    //
                    // Howevever, considering how a fleet of devices connected to a hub may behave together, one must consider the implication of performing
                    // work on a device (e.g., get twin) when it comes online. If all the devices go offline and then come online at the same time (for example,
                    // during a servicing event) it could introduce increased latency or even throttling responses.
                    // For more information, see https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#traffic-shaping.
                    await GetTwinAndDetectChangesAsync(s_cancellationTokenSource.Token);
                    _logger.LogDebug("The client has retrieved twin values after the connection status changes into CONNECTED.");
                    break;

                case ConnectionStatus.DisconnectedRetrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance.");
                    break;

                case ConnectionStatus.Closed:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (connectionInfo.ChangeReason)
                    {
                        case ConnectionStatusChangeReason.BadCredential:
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                _logger.LogWarning($"The current connection string is invalid. Trying another.");

                                try
                                {
                                    await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                                }
                                catch (OperationCanceledException) { } // User canceled

                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.DeviceDisabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.RetryExpired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            try
                            {
                                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException) { } // User canceled

                            break;

                        case ConnectionStatusChangeReason.CommunicationError:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            try
                            {
                                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException) { } // User canceled

                            break;

                        default:
                            _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;
                    }

                    break;

                default:
                    _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        private async Task GetTwinAndDetectChangesAsync(CancellationToken cancellationToken)
        {
            Twin twin = null;

            // Allow a single thread to call GetTwin here
            await _initSemaphore.WaitAsync(cancellationToken);

            try
            {
                // For the following call, we execute with a retry strategy with incrementally increasing delays between retry.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "GetTwin",
                    asyncOperation: async () =>
                    {
                        twin = await s_deviceClient.GetTwinAsync();
                        _logger.LogInformation($"Device retrieving twin values: {twin.ToJson()}");

                        TwinCollection twinCollection = twin.Properties.Desired;
                        long serverDesiredPropertyVersion = twinCollection.Version;

                        // Check if the desired property version is outdated on the local side.
                        if (serverDesiredPropertyVersion > s_localDesiredPropertyVersion)
                        {
                            _logger.LogDebug($"The desired property version cached on local is changing from {s_localDesiredPropertyVersion} to {serverDesiredPropertyVersion}.");
                            await HandleTwinUpdateNotificationsAsync(twinCollection);
                        }
                    },
                    shouldExecuteOperation: () => IsDeviceConnected,
                    logger: _logger,
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest)
        {
            CancellationToken cancellationToken = s_cancellationTokenSource.Token;

            if (!cancellationToken.IsCancellationRequested)
            {
                var reportedProperties = new TwinCollection();

                _logger.LogInformation($"Twin property update requested: \n{twinUpdateRequest.ToJson()}");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                foreach (KeyValuePair<string, object> desiredProperty in twinUpdateRequest)
                {
                    _logger.LogInformation($"Setting property {desiredProperty.Key} to {desiredProperty.Value}.");
                    reportedProperties[desiredProperty.Key] = desiredProperty.Value;
                }

                s_localDesiredPropertyVersion = twinUpdateRequest.Version;
                _logger.LogDebug($"The desired property version on local is currently {s_localDesiredPropertyVersion}.");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "UpdateReportedProperties",
                    asyncOperation: async () => await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken),
                    shouldExecuteOperation: () => IsDeviceConnected,
                    logger: _logger,
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    _logger.LogInformation($"Device sending message {++messageCount} to IoT hub.");

                    Message message = PrepareMessage(messageCount);
                    await RetryOperationHelper.RetryTransientExceptionsAsync(
                        operationName: $"SendD2CMessage_{messageCount}",
                        asyncOperation: async () => await s_deviceClient.SendEventAsync(message),
                        shouldExecuteOperation: () => IsDeviceConnected,
                        logger: _logger,
                        exceptionsToBeIgnored: _exceptionsToBeIgnored,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation($"Device sent message {messageCount} to IoT hub.");
                }

                await Task.Delay(s_sleepDuration, cancellationToken);
            }
        }

        private Task<MessageAcknowledgement> OnMessageReceivedAsync(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.Payload);
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

            foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
            }
            _logger.LogInformation(formattedMessage.ToString());

            _logger.LogInformation($"Completed message [{messageData}].");
            return Task.FromResult(MessageAcknowledgement.Complete);
        }

        private static Message PrepareMessage(int messageId)
        {
            int temperature = s_randomGenerator.Next(20, 35);
            int humidity = s_randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = messageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");

            return eventMessage;
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
        {
            return (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Closed)
                && _deviceConnectionStrings.Any();
        }
    }
}
