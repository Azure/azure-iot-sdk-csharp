// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSample
    {
        private static readonly Random s_randomGenerator = new();
        private static readonly SemaphoreSlim s_initSemaphore = new(1, 1);
        private static readonly ClientOptions s_clientOptions = new() { SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset };

        private readonly Parameters _parameters;
        private readonly TimeSpan s_waitTIme;
        private readonly ILogger _logger;
        private static X509Certificate2 s_certificate;
        private static SecurityProviderX509Certificate s_security;
        private static DeviceAuthenticationWithX509Certificate s_auth;

        // An UnauthorizedException is handled in the connection status change handler through its corresponding status change event.
        // We will ignore this exception when thrown by client API operations.
        private static readonly HashSet<Type> s_exceptionsToBeRetried = new()
        {
            // Unauthorized exception conditions are handled by the ConnectionStatusChangeHandler in this sample and the sample will try
            // to reconnect indefinitely, so don't give up on an operation that sees this exception.
            typeof(UnauthorizedException),
        };
        private readonly IRetryPolicy _customRetryPolicy;


        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile DeviceClient s_deviceClient;
        private static volatile ConnectionStatus s_connectionStatus = ConnectionStatus.Disconnected;
        private static String s_hub;

        private static CancellationTokenSource s_appCancellation;

        // A safe initial value for caching the twin desired properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localDesiredPropertyVersion = 1;

        public DeviceReconnectionSample(Parameters parameters, ILogger logger)
        {
            _logger = logger;
            _parameters = parameters;

            _customRetryPolicy = new CustomRetryPolicy(s_exceptionsToBeRetried, _logger, _parameters.ExponentOrderBackoff);
            s_waitTIme = TimeSpan.FromSeconds(_parameters.ConnectionWaitTime);
        }

        private static bool IsDeviceConnected => s_connectionStatus == ConnectionStatus.Connected;

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            _logger.LogInformation($"Loading the certificate...");
            s_certificate = LoadProvisioningCertificate(_parameters.GetCertificatePath(), _parameters.CertificateName, _parameters.CertificatePassword);
            s_security = new SecurityProviderX509Certificate(s_certificate);
            
            using ProvisioningTransportHandler transport = GetTransportHandler(_parameters.TransportType);
            if (ImplementsWebProxy(_parameters.TransportType) && !String.IsNullOrEmpty(_parameters.ProxyServerAddress.Trim()))
            {
                transport.Proxy = _parameters.ProxyServerAddress == null
                    ? null
                    : new WebProxy(_parameters.ProxyServerAddress);
                _logger.LogInformation($"Provisioning with {_parameters.TransportType} tranport thru proxy {_parameters.ProxyServerAddress}.");
            }
            else
            {
                _logger.LogInformation($"Provisioning with {_parameters.TransportType} transport.");
            }

            var provClient = ProvisioningDeviceClient.Create(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                s_security,
                transport);

            _logger.LogInformation($"Provisioning with Device: {s_security.GetRegistrationID()}.");

            DeviceRegistrationResult result = null;
            s_appCancellation = new CancellationTokenSource(sampleRunningTime);

            // fixed duration loop retry for provisioning
            int tryCount = 0;
            while (true)
            {
                try
                {
                    _logger.LogInformation($"Provisioning Attempt #{tryCount} with {_parameters.GlobalDeviceEndpoint}... ");
                    result = s_waitTIme != TimeSpan.MaxValue
                        ? await provClient.RegisterAsync(s_waitTIme).ConfigureAwait(false)
                        : await provClient.RegisterAsync(s_appCancellation.Token).ConfigureAwait(false);
                    break;
                }
                // Catching all ProvisioningTransportException as the status code is not the same for Mqtt, Amqp and Http.
                // It should be safe to retry on any non-transient exception just for E2E tests as we have concurrency issues.
                catch (ProvisioningTransportException ex) when (tryCount < int.MaxValue)
                {
                    _logger.LogError($"Provisioning Attempt #{tryCount++} failed. Retry in {s_waitTIme}s. Reason: [{ex.GetType()}: {ex.Message}]");
                    await Task.Delay(TimeSpan.FromSeconds(_parameters.ConnectionWaitTime)).ConfigureAwait(false);
                }
            }

            _logger.LogInformation($"Device {result.DeviceId} registration {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                throw new ArgumentException("Registration status did not assign a hub, so exiting this sample.");
            }

            _logger.LogInformation($"Device {result.DeviceId} registered to {result.AssignedHub}.");
            s_hub = result.AssignedHub;

            _logger.LogInformation("Creating X509 authentication for IoT Hub...");
            s_auth = new DeviceAuthenticationWithX509Certificate(
                result.DeviceId,
                s_certificate);
        
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
                await Task.WhenAll(SendMessagesAsync(s_appCancellation.Token), ReceiveMessagesAsync(s_appCancellation.Token));
            }
            catch (OperationCanceledException) { } // User canceled the operation
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting: \n{ex}");
                s_appCancellation.Cancel();
            }

            // Finally, close the client, but we can't use s_appCancellation because it has been signaled to quit the app.
            await s_deviceClient.CloseAsync(CancellationToken.None);
            s_initSemaphore.Dispose();
            s_appCancellation.Dispose();
        }

        private async Task InitializeAndSetupClientAsync(CancellationToken cancellationToken)
        {
            if (ShouldClientBeInitialized(s_connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                await s_initSemaphore.WaitAsync(cancellationToken);
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
                            catch (UnauthorizedException) { } // If the previous token is now invalid, this call may fail
                            s_deviceClient.Dispose();
                        }

                        s_deviceClient = DeviceClient.Create(s_hub, s_auth, _parameters.TransportType);
                        s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandlerAsync);
                        s_deviceClient.SetRetryPolicy(_customRetryPolicy);
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

                // You will need to subscribe to the client callbacks any time the client is initialized.
                await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, null, cancellationToken);
                _logger.LogDebug("The client has subscribed to desired property update notifications.");
            }
        }

        // It is not generally a good practice to have async void methods, however, DeviceClient.ConnectionStatusChangeHandlerAsync() event handler signature
        // has a void return type. As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock before attempting to
        // initialize or dispose the device client instance; the async method GetTwinAndDetectChangesAsync is implemented similarly for the same purpose.
        private async void ConnectionStatusChangeHandlerAsync(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");
            s_connectionStatus = status;

            switch (status)
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
                    await GetTwinAndDetectChangesAsync(s_appCancellation.Token);
                    _logger.LogDebug("The client has retrieved twin values after the connection status changes into CONNECTED.");
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance.");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            if (!String.IsNullOrEmpty(s_hub.Trim()))
                            {
                                _logger.LogWarning("### The DeviceClient has been disconnected because of bad credential." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                                try
                                {
                                    await InitializeAndSetupClientAsync(s_appCancellation.Token);
                                }
                                catch (OperationCanceledException) { } // User canceled

                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            s_appCancellation.Cancel();
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            s_appCancellation.Cancel();
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            try
                            {
                                await InitializeAndSetupClientAsync(s_appCancellation.Token);
                            }
                            catch (OperationCanceledException) { } // User canceled

                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            try
                            {
                                await InitializeAndSetupClientAsync(s_appCancellation.Token);
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
            // For the following call, we execute with a retry strategy with incrementally increasing delays between retry.
            Twin twin = await s_deviceClient.GetTwinAsync(cancellationToken);
            _logger.LogInformation($"Device retrieving twin values: {twin.ToJson()}");

            TwinCollection twinCollection = twin.Properties.Desired;
            long serverDesiredPropertyVersion = twinCollection.Version;

            // Check if the desired property version is outdated on the local side.
            if (serverDesiredPropertyVersion > s_localDesiredPropertyVersion)
            {
                _logger.LogDebug($"The desired property version cached on local is changing from {s_localDesiredPropertyVersion} to {serverDesiredPropertyVersion}.");
                await HandleTwinUpdateNotificationsAsync(twinCollection, cancellationToken);
            }
        }

        private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest, object userContext)
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

            try
            {
                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties, s_appCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Fail gracefully on sample exit.
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
                    await s_deviceClient.SendEventAsync(message, cancellationToken);
                    _logger.LogInformation($"Device sent message {messageCount} to IoT hub.");
                }

                await Task.Delay(s_waitTIme, cancellationToken);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsDeviceConnected)
                {
                    await Task.Delay(s_waitTIme, cancellationToken);
                    continue;
                }
                else if (_parameters.TransportType == TransportType.Http1)
                {
                    // The call to ReceiveAsync over HTTP completes immediately, rather than waiting up to the specified
                    // time or when a cancellation token is signaled, so if we want it to poll at the same rate, we need
                    // to add an explicit delay here.
                    await Task.Delay(s_waitTIme, cancellationToken);
                }

                _logger.LogInformation($"Device waiting for C2D messages from the hub for {s_waitTIme}." +
                    $"\nUse the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                await ReceiveMessageAndCompleteAsync(cancellationToken);
            }
        }

        private async Task ReceiveMessageAndCompleteAsync(CancellationToken cancellationToken)
        {
            Message receivedMessage = null;
            try
            {
                receivedMessage = await s_deviceClient.ReceiveAsync(cancellationToken);
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

            using (receivedMessage)
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                var formattedMessage = new StringBuilder($"Received message '{receivedMessage.MessageId}': [{messageData}]");

                foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
                {
                    formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
                }
                _logger.LogInformation(formattedMessage.ToString());

                try
                {
                    await s_deviceClient.CompleteAsync(receivedMessage, cancellationToken);
                    _logger.LogInformation($"Completed message '{receivedMessage.MessageId}'.");
                }
                catch (DeviceMessageLockLostException)
                {
                    _logger.LogWarning($"Took too long to process and complete a C2D message; it will be redelivered.");
                }
            }
        }

        private static Message PrepareMessage(int messageId)
        {
            const int temperatureThreshold = 30;

            int temperature = s_randomGenerator.Next(20, 35);
            int humidity = s_randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = messageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > temperatureThreshold) ? "true" : "false");

            return eventMessage;
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
        {
            return (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled)
                && !String.IsNullOrEmpty(s_hub.Trim());
        }

        private ProvisioningTransportHandler GetTransportHandler(TransportType type)
        {
            return type switch
            {
                TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
                TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
                TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
                TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
                TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
                TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
                TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
                _ => throw new NotSupportedException($"Unsupported provisioning transport type {type}"),
            };
        }
        private X509Certificate2 LoadProvisioningCertificate(String path, String file, String password)
        {
            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(
                path,
                password,
                X509KeyStorageFlags.UserKeySet);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                _logger.LogInformation($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{file} did not contain any certificate with a private key.");
            }

            _logger.LogInformation($"Using certificate {certificate.Thumbprint} {certificate.Subject}");

            return certificate;
        }

        public static bool ImplementsWebProxy(TransportType transportProtocol)
        {
            switch (transportProtocol)
            {
                case TransportType.Mqtt_WebSocket_Only:
                case TransportType.Amqp_WebSocket_Only:
                    return true;

                case TransportType.Amqp:
                case TransportType.Amqp_Tcp_Only:
                case TransportType.Mqtt:
                case TransportType.Mqtt_Tcp_Only:
                case TransportType.Http1:
                default:
                    return false;
            }

            throw new NotSupportedException($"Unknown transport: '{transportProtocol}'.");
        }
    }
}
