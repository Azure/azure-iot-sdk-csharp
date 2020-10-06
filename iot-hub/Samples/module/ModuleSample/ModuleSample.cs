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
    public class ModuleSample
    {
        private static readonly Random s_randomGenerator = new Random();
        private const int TemperatureThreshold = 30;

        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromHours(1);

        private readonly object _initLock = new object();
        private readonly List<string> _moduleConnectionStrings;
        private readonly TransportType _transportType;

        private readonly ILogger _logger;
        private static ModuleClient s_moduleClient;

        private static ConnectionStatus s_connectionStatus;
        private static bool s_wasEverConnected;

        public ModuleSample(List<string> moduleConnectionStrings, TransportType transportType, ILogger logger)
        {
            _logger = logger;

            // This class takes a list of potentially valid connection strings (most likely the currently known good primary and secondary keys)
            // and will attempt to connect with the first. If it receives feedback that a connection string is invalid, it will discard it, and
            // if any more are remaining, will try the next one.
            // To test this, either pass an invalid connection string as the first one, or rotate it while the sample is running, and wait about
            // 5 minutes.
            if (moduleConnectionStrings == null
                || !moduleConnectionStrings.Any())
            {
                throw new ArgumentException("At least one connection string must be provided.", nameof(moduleConnectionStrings));
            }
            _moduleConnectionStrings = moduleConnectionStrings;
            _logger.LogInformation($"Supplied with {_moduleConnectionStrings.Count} connection string(s).");

            _transportType = transportType;
            _logger.LogInformation($"Using {_transportType} transport.");

            InitializeClient();
        }

        public async Task RunSampleAsync()
        {
            _logger.LogInformation(">> Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation(">> Sample execution cancellation requested; will exit.");
            };

            try
            {
                await s_moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, cts.Token, cts.Token);
                await SendMessagesAsync(cts.Token);
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
                && _moduleConnectionStrings.Any())
            {
                lock (_initLock)
                {
                    _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                    // If the module client instance has been previously initialized, then dispose it.
                    // The s_wasEverConnected variable is required to store if the client ever reported Connected status.
                    if (s_wasEverConnected && s_connectionStatus == ConnectionStatus.Disconnected)
                    {
                        s_moduleClient?.Dispose();
                        s_wasEverConnected = false;
                    }

                    s_moduleClient = ModuleClient.CreateFromConnectionString(_moduleConnectionStrings.First(), _transportType);
                    s_moduleClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    s_moduleClient.OperationTimeoutInMilliseconds = (uint)s_operationTimeout.TotalMilliseconds;
                }

                try
                {
                    // Force connection now
                    s_moduleClient.OpenAsync().GetAwaiter().GetResult();
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
                    _logger.LogDebug("### The ModuleClient is CONNECTED; all operations will be carried out as normal.");

                    s_wasEverConnected = true;
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The ModuleClient is retrying based on the retry policy. Do NOT close or open the ModuleClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The ModuleClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the module client," +
                        " you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            string badCs = _moduleConnectionStrings[0];
                            _moduleConnectionStrings.RemoveAt(0);
                            if (_moduleConnectionStrings.Any())
                            {
                                // Not great to print out a connection string, but this is done for sample/demo purposes.
                                _logger.LogWarning($"The current connection string {badCs} is invalid. Trying another.");
                                InitializeClient();
                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The module has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the module status in Azure and then create a new module client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The ModuleClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the module client," +
                                " you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The ModuleClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the module client," +
                                " you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                            break;

                        default:
                            _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected," +
                                " contact the client library team with logs.");
                            break;
                    }

                    break;

                default:
                    _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected," +
                        " contact the client library team with logs.");
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
                    _logger.LogInformation($"Module sending message {++messageCount} to IoT Hub...");

                    (Message message, string payload) = PrepareMessage(messageCount);
                    while (true)
                    {
                        try
                        {
                            await s_moduleClient.SendEventAsync(message);
                            _logger.LogInformation($"Sent message {messageCount} of {payload}");
                            message.Dispose();
                            break;
                        }
                        catch (IotHubException ex) when (ex.IsTransient)
                        {
                            // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                            _logger.LogError($"An IotHubException was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                        {
                            _logger.LogError($"A network related exception was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unexpected error {ex}");
                        }

                        // wait and retry
                        await Task.Delay(s_sleepDuration);
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private (Message, string) PrepareMessage(int messageId)
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

            return (eventMessage, messagePayload);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            var cancellationToken = (CancellationToken)userContext;
            _logger.LogInformation($"Desired property changed: {desiredProperties.ToJson()}");

            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTimeOffset.Now.ToUniversalTime();

            // If cancellation has not been requested, and the device is connected, then send the reported property update.
            while (!cancellationToken.IsCancellationRequested)
            {
                if (s_connectionStatus == ConnectionStatus.Connected)
                {
                    try
                    {
                        await s_moduleClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
                        _logger.LogInformation($"Sent current time as reported property update: {reportedProperties.ToJson()}");
                        break;
                    }
                    catch (IotHubException ex) when (ex.IsTransient)
                    {
                        // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                        _logger.LogError($"An IotHubException was caught, but will try to recover and retry: {ex}");
                    }
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry: {ex}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Unexpected error {ex}");
                    }

                    // wait and retry
                    await Task.Delay(s_sleepDuration);
                }

                // wait and retry
                await Task.Delay(s_sleepDuration);
            }
        }
    }
}
