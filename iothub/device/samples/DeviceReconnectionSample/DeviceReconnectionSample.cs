// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class DeviceReconnectionSample
    {
        private const int TemperatureThreshold = 30;
        private static readonly Random s_randomGenerator = new();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly List<string> _deviceConnectionStrings;
        private readonly IotHubClientOptions _clientOptions;

        // An UnauthorizedException is handled in the connection status change handler through its corresponding status change event.
        // We will ignore this exception when thrown by client API operations.
        private readonly Dictionary<Type, string> _exceptionsToBeIgnored = new()
        {
            { typeof(UnauthorizedException), "Unauthorized exceptions are handled by the ConnectionStatusChangeHandler." }
        };

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile IotHubDeviceClient s_deviceClient;

        private static CancellationTokenSource s_cancellationTokenSource;

        // A safe initial value for caching the twin desired properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localDesiredPropertyVersion = 1;

        internal DeviceReconnectionSample(ApplicationParameters parameters)
        {
            List<string> deviceConnectionStrings = parameters.GetConnectionStrings();

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
            Console.WriteLine($"Supplied with {_deviceConnectionStrings.Count} connection string(s).");

            _clientOptions = parameters.Transport switch
            {
                Transport.Mqtt => new IotHubClientOptions(new IotHubClientMqttSettings(parameters.Protocol)),
                Transport.Amqp => new IotHubClientOptions(new IotHubClientAmqpSettings(parameters.Protocol)),
                _ => throw new ArgumentException($"Unsupported transport of {parameters.Transport}"),
            };
            Console.WriteLine($"Using {parameters.Transport} transport with {parameters.Protocol} protocol.");

            _clientOptions.SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset;
        }

        private static bool IsDeviceConnected => s_deviceClient.ConnectionInfo.Status == ConnectionStatus.Connected;

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            s_cancellationTokenSource = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_cancellationTokenSource.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            Console.WriteLine($"Sample execution started, press Control+C to quit the sample.");

            try
            {
                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                await SendMessagesAsync(s_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // User canceled the operation. Nothing to do here.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unrecoverable exception caught, user action is required, so exiting: \n{ex}");
                s_cancellationTokenSource.Cancel();
            }

            _initSemaphore.Dispose();
            s_cancellationTokenSource.Dispose();
        }

        private async Task InitializeAndSetupClientAsync(CancellationToken cancellationToken)
        {
            if (ShouldClientBeInitialized())
            {
                // Allow a single thread to dispose and initialize the client instance.
                await _initSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (ShouldClientBeInitialized())
                    {
                        var status = ConnectionStatus.Disconnected;
                        if (s_deviceClient != null)
                        {
                            status = s_deviceClient.ConnectionInfo.Status;
                        }

                        Console.WriteLine($"Attempting to initialize the client instance, current status={status}");

                        // If the device client instance has been previously initialized, close and dispose it.
                        if (s_deviceClient != null)
                        {
                            try
                            {
                                await s_deviceClient.CloseAsync(cancellationToken);
                            }
                            catch (UnauthorizedException) { } // if the previous token is now invalid, this call may fail
                            s_deviceClient.Dispose();
                        }

                        s_deviceClient = IotHubDeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), _clientOptions);
                        s_deviceClient.SetConnectionStatusChangeHandler(ConnectionStatusChangeHandler);
                        await s_deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, null, s_cancellationTokenSource.Token);
                        Console.WriteLine("Initialized the client instance.");
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
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
                Console.WriteLine($"The client instance has been opened.");

                // You will need to subscribe to the client callbacks any time the client is initialized.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "SubscribeTwinUpdates",
                    asyncOperation: async () => await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, cancellationToken),
                    shouldExecuteOperation: () => IsDeviceConnected,
                    exceptionsToBeIgnored: _exceptionsToBeIgnored,
                    cancellationToken: cancellationToken);
                Console.WriteLine("The client has subscribed to desired property update notifications.");
            }
        }

        private async Task OnC2dMessageReceivedAsync(Message message, object context)
        {
            string messageData = Encoding.ASCII.GetString(message.Payload);
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

            foreach (var prop in message.Properties)
            {
                formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
            }
            Console.WriteLine(formattedMessage.ToString());

            await s_deviceClient.CompleteMessageAsync(message);
            Console.WriteLine($"Completed message [{messageData}].");
        }

        // It is not generally a good practice to have async void methods, however, DeviceClient.SetConnectionStatusChangesHandler() event handler signature
        // has a void return type. As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock before attempting to
        // initialize or dispose the device client instance; the async method GetTwinAndDetectChangesAsync is implemented similarly for the same purpose.
        private async void ConnectionStatusChangeHandler(ConnectionInfo connectionInfo)
        {
            var status = connectionInfo.Status;
            var reason = connectionInfo.ChangeReason;
            Console.WriteLine($"Connection status changed: status={status}, reason={reason}");

            switch (status)
            {
                case ConnectionStatus.Connected:
                    Console.WriteLine("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    // Call GetTwinAndDetectChangesAsync() to retrieve twin values from the server once the connection status changes into Connected.
                    // This can get back "lost" twin updates in a device reconnection from status like Disconnected_Retrying or Disconnected.
                    await GetTwinAndDetectChangesAsync(s_cancellationTokenSource.Token);
                    Console.WriteLine("The client has retrieved twin values after the connection status changes into CONNECTED.");
                    break;

                case ConnectionStatus.DisconnectedRetrying:
                    Console.WriteLine("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance.");
                    break;

                case ConnectionStatus.Disabled:
                    Console.WriteLine("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.BadCredential:
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                Console.WriteLine($"The current connection string is invalid. Trying another.");
                                await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                                break;
                            }

                            Console.WriteLine("### The supplied credentials are invalid. Update the parameters and run again.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.DeviceDisabled:
                            Console.WriteLine("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            s_cancellationTokenSource.Cancel();
                            break;

                        case ConnectionStatusChangeReason.RetryExpired:
                            Console.WriteLine("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                            break;

                        case ConnectionStatusChangeReason.CommunicationError:
                            Console.WriteLine("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndSetupClientAsync(s_cancellationTokenSource.Token);
                            break;

                        default:
                            Console.WriteLine("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;
                    }

                    break;

                default:
                    Console.WriteLine("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        private async Task GetTwinAndDetectChangesAsync(CancellationToken cancellationToken)
        {
            Twin twin = null;

            // Allow a single thread to call GetTwin here
            await _initSemaphore.WaitAsync(cancellationToken);

            await RetryOperationHelper.RetryTransientExceptionsAsync(
                operationName: "GetTwin",
                asyncOperation: async () =>
                {
                    twin = await s_deviceClient.GetTwinAsync();
                    Console.WriteLine($"Device retrieving twin values: {twin.ToJson()}");

                    TwinCollection twinCollection = twin.Properties.Desired;
                    long serverDesiredPropertyVersion = twinCollection.Version;

                    // Check if the desired property version is outdated on the local side.
                    if (serverDesiredPropertyVersion > s_localDesiredPropertyVersion)
                    {
                        Console.WriteLine($"The desired property version cached on local is changing from {s_localDesiredPropertyVersion} to {serverDesiredPropertyVersion}.");
                        await HandleTwinUpdateNotificationsAsync(twinCollection, cancellationToken);
                    }
                },
                shouldExecuteOperation: () => IsDeviceConnected,
                exceptionsToBeIgnored: _exceptionsToBeIgnored,
                cancellationToken: cancellationToken);

            _initSemaphore.Release();
        }

        private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest, object userContext)
        {
            CancellationToken cancellationToken = (CancellationToken)userContext;

            if (!cancellationToken.IsCancellationRequested)
            {
                var reportedProperties = new TwinCollection();

                Console.WriteLine($"Twin property update requested: \n{twinUpdateRequest.ToJson()}");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                foreach (KeyValuePair<string, object> desiredProperty in twinUpdateRequest)
                {
                    Console.WriteLine($"Setting property {desiredProperty.Key} to {desiredProperty.Value}.");
                    reportedProperties[desiredProperty.Key] = desiredProperty.Value;
                }

                s_localDesiredPropertyVersion = twinUpdateRequest.Version;
                Console.WriteLine($"The desired property version on local is currently {s_localDesiredPropertyVersion}.");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                await RetryOperationHelper.RetryTransientExceptionsAsync(
                    operationName: "UpdateReportedProperties",
                    asyncOperation: async () => await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken),
                    shouldExecuteOperation: () => IsDeviceConnected,
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
                    Console.WriteLine($"Device sending telemetry message {++messageCount} to IoT hub.");

                    Message message = PrepareTelemetryMessage(messageCount);
                    await RetryOperationHelper.RetryTransientExceptionsAsync(
                        operationName: $"SendTelemetryMessage_{messageCount}",
                        asyncOperation: async () => await s_deviceClient.SendEventAsync(message),
                        shouldExecuteOperation: () => IsDeviceConnected,
                        exceptionsToBeIgnored: _exceptionsToBeIgnored,
                        cancellationToken: cancellationToken);

                    Console.WriteLine($"Device sent telemetry message {messageCount} to IoT hub.");
                }

                await Task.Delay(s_sleepDuration, cancellationToken);
            }
        }

        private static Message PrepareTelemetryMessage(int messageId)
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

        // If the client reports Connected status, it is already in operational status.
        // If the client reports DisconnectedRetrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private bool ShouldClientBeInitialized()
        {
            return (s_deviceClient == null) 
                || (s_deviceClient.ConnectionInfo.Status == ConnectionStatus.Disconnected || s_deviceClient.ConnectionInfo.Status == ConnectionStatus.Disabled)
                && _deviceConnectionStrings.Any();
        }
    }
}
