// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSample
    {
        private const int TemperatureThreshold = 30;
        private static readonly Random s_randomGenerator = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private readonly List<string> _deviceConnectionStrings;
        private readonly TransportType _transportType;
        private readonly ClientOptions _clientOptions = new ClientOptions { SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset };

        // An UnauthorizedException is handled in the connection status change handler through its corresponding status change event.
        // We will ignore this exception when thrown by the client API operation.
        private readonly Dictionary<Type, string> _exceptionsToBeIgnored = new Dictionary<Type, string> { { typeof(UnauthorizedException), "Unauthorized exceptions are handled by the ConnectionStatusChangeHandler." } };

        private readonly ILogger _logger;

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile DeviceClient s_deviceClient;
        private static volatile ConnectionStatus s_connectionStatus = ConnectionStatus.Disconnected;

        private static CancellationTokenSource s_cancellationTokenSource;

        public DeviceReconnectionSample(List<string> deviceConnectionStrings, TransportType transportType, ILogger logger)
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

            _transportType = transportType;
            _logger.LogInformation($"Using {_transportType} transport.");
        }

        private static bool IsDeviceConnected => s_connectionStatus == ConnectionStatus.Connected;

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            s_cancellationTokenSource = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_cancellationTokenSource.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation($"Sample execution started, press Control+C to quit the sample.");

            try
            {
                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                await Task.WhenAll(SendMessagesAsync(s_cancellationTokenSource.Token), ReceiveMessagesAsync(s_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                // User canceled the operation. Nothing to do here.
            }
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

                        // If the device client instance has been previously initialized, then dispose it.
                        if (s_deviceClient != null)
                        {
                            await s_deviceClient.CloseAsync(cancellationToken);
                            s_deviceClient.Dispose();
                            s_deviceClient = null;
                        }

                        s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), _transportType, _clientOptions);
                        s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
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

        // It is not good practice to have async void methods, however, DeviceClient.SetConnectionStatusChangesHandler() event handler signature has a void return type.
        // As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock
        // before attempting to initialize or dispose the device client instance.
        private async void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");
            s_connectionStatus = status;

            switch (status)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                _logger.LogWarning($"The current connection string is invalid. Trying another.");
                                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
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

        private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest, object userContext)
        {
            CancellationToken cancellationToken = (CancellationToken)userContext;

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

                    using Message message = PrepareMessage(messageCount);
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

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var c2dReceiveExceptionsToBeIgnored = new Dictionary<Type, string>(_exceptionsToBeIgnored)
            {
                { typeof(DeviceMessageLockLostException), "Attempted to complete a received message whose lock token has expired" }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsDeviceConnected)
                {
                    await Task.Delay(s_sleepDuration, cancellationToken);
                    continue;
                }
                else if (_transportType == TransportType.Http1)
                {
                    // The call to ReceiveAsync over HTTP completes immediately, rather than waiting up to the specified
                    // time or when a cancellation token is signaled, so if we want it to poll at the same rate, we need
                    // to add an explicit delay here.
                    await Task.Delay(s_sleepDuration, cancellationToken);
                }

                _logger.LogInformation($"Device waiting for C2D messages from the hub for {s_sleepDuration}." +
                    $"\nUse the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "ReceiveAndCompleteC2DMessage",
                    asyncOperation: async () => await ReceiveMessageAndCompleteAsync(),
                    shouldExecuteOperation: () => IsDeviceConnected,
                    logger: _logger,
                    exceptionsToBeIgnored: c2dReceiveExceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ReceiveMessageAndCompleteAsync()
        {
            using var cts = new CancellationTokenSource(s_sleepDuration);
            Message receivedMessage = null;
            try
            {
                receivedMessage = await s_deviceClient.ReceiveAsync(cts.Token);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                _logger.LogInformation("Timed out waiting to receive a message.");
            }

            if (receivedMessage == null)
            {
                _logger.LogInformation("No message received.");
                return;
            }

            try
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

                foreach (var prop in receivedMessage.Properties)
                {
                    formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
                }
                _logger.LogInformation(formattedMessage.ToString());

                await s_deviceClient.CompleteAsync(receivedMessage);
                _logger.LogInformation($"Completed message [{messageData}].");
            }
            finally
            {
                receivedMessage.Dispose();
            }
        }

        private static Message PrepareMessage(int messageId)
        {
            var temperature = s_randomGenerator.Next(20, 35);
            var humidity = s_randomGenerator.Next(60, 80);
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
            return (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled)
                && _deviceConnectionStrings.Any();
        }
    }
}
