// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
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
        private static readonly Random s_randomGenerator = new Random();
        private const int TemperatureThreshold = 30;

        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromHours(1);

        private readonly object _initLock = new object();
        private readonly List<string> _deviceConnectionStrings;
        private readonly TransportType _transportType;

        private readonly ILogger _logger;
        private static DeviceClient s_deviceClient;

        private static ConnectionStatus s_connectionStatus;
        private static bool s_wasEverConnected;

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
            _transportType = transportType;

            InitializeClient();
        }

        public async Task RunSampleAsync()
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            try
            {
                await Task.WhenAll(SendMessagesAsync(cts.Token), ReceiveMessagesAsync(cts.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }
        }

        private void InitializeClient()
        {
            // If the client reports Connected status, it is already in operational state.
            if (s_connectionStatus != ConnectionStatus.Connected
                && _deviceConnectionStrings.Any())
            {
                lock (_initLock)
                {
                    _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                    // If the device client instance has been previously initialized, then dispose it.
                    // The s_wasEverConnected variable is required to store if the client ever reported Connected status.
                    if (s_wasEverConnected && s_connectionStatus == ConnectionStatus.Disconnected)
                    {
                        s_deviceClient?.Dispose();
                        s_wasEverConnected = false;
                    }

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), _transportType);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    s_deviceClient.OperationTimeoutInMilliseconds = (uint)s_operationTimeout.TotalMilliseconds;
                }

                try
                {
                    // Force connection now
                    s_deviceClient.OpenAsync().GetAwaiter().GetResult();
                    _logger.LogDebug($"Initialized the client instance.");
                }
                catch (UnauthorizedException)
                {
                    // Handled by the ConnectionStatusChangeHandler
                }
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");

            s_connectionStatus = status;
            switch (s_connectionStatus)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    s_wasEverConnected = true;
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
                            string badCs = _deviceConnectionStrings[0];
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                // Not great to print out a connection string, but this is done for sample/demo purposes.
                                _logger.LogWarning($"The current connection string {badCs} is invalid. Trying another.");
                                InitializeClient();
                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
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

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (s_connectionStatus == ConnectionStatus.Connected)
                {
                    _logger.LogInformation($"Device sending message {++messageCount} to IoT Hub...");

                    try
                    {
                        await SendMessageAsync(messageCount);
                    }
                    catch (IotHubException ex) when (ex.IsTransient)
                    {
                        // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                        _logger.LogError($"An IotHubException was caught, but will try to recover and retry explicitly: {ex}");
                    }
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (s_connectionStatus != ConnectionStatus.Connected)
                {
                    await Task.Delay(s_sleepDuration);
                    continue;
                }

                _logger.LogInformation($"Device waiting for C2D messages from the hub - for {s_sleepDuration}...");
                _logger.LogInformation("Use the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                try
                {
                    await ReceiveMessageAndCompleteAsync();
                }
                catch (DeviceMessageLockLostException ex)
                {
                    _logger.LogWarning($"Attempted to complete a received message whose lock token has expired; ignoring: {ex}");
                }
                catch (IotHubException ex) when (ex.IsTransient)
                {
                    // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                    _logger.LogError($"An IotHubException was caught, but will try to recover and retry explicitly: {ex}");
                }
                catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                {
                    _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
                }
            }
        }

        private async Task SendMessageAsync(int messageId)
        {
            var temperature = s_randomGenerator.Next(20, 35);
            var humidity = s_randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            using var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = messageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");

            await s_deviceClient.SendEventAsync(eventMessage);
            _logger.LogInformation($"Sent message {messageId} with data [{messagePayload}]");
        }

        private async Task ReceiveMessageAndCompleteAsync()
        {
            using Message receivedMessage = await s_deviceClient.ReceiveAsync(s_sleepDuration);
            if (receivedMessage == null)
            {
                _logger.LogInformation("No message received; timed out.");
                return;
            }

            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

            foreach (var prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
            }
            _logger.LogInformation(formattedMessage.ToString());

            await s_deviceClient.CompleteAsync(receivedMessage);
            _logger.LogInformation($"Completed message [{messageData}].");
        }
    }
}
