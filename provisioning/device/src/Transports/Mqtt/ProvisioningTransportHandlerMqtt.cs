// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Provisioning.Client.Transports.Mqtt;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the MQTT protocol implementation for the provisioning transport handler.
    /// </summary>
    internal class ProvisioningTransportHandlerMqtt : ProvisioningTransportHandler
    {
        private const int MqttTcpPort = 8883;
        private const string UsernameFormat = "{0}/registrations/{1}/api-version={2}&ClientVersion={3}";
        private const string SubscribeFilter = "$dps/registrations/res/#";
        private const string RegisterTopic = "$dps/registrations/PUT/iotdps-register/?$rid={0}";
        private const string GetOperationsTopic = "$dps/registrations/GET/iotdps-get-operationstatus/?$rid={0}&operationId={1}";
        private const string BasicProxyAuthentication = "Basic";

        private static readonly TimeSpan s_defaultOperationPollingInterval = TimeSpan.FromSeconds(2);

        private readonly MqttFactory s_mqttFactory = new MqttFactory(new MqttLogger());

        private TaskCompletionSource<RegistrationOperationStatus> _startProvisioningRequestStatusSource;
        private TaskCompletionSource<RegistrationOperationStatus> _checkRegistrationOperationStatusSource;
        private CancellationTokenSource _connectionLostCancellationToken;
        private int _packetId;
        private bool _isOpening;
        private bool _isClosing;
        private Exception _connectionLossCause;

        private readonly ProvisioningClientOptions _options;
        private readonly ProvisioningClientMqttSettings _settings;
        private readonly MqttQualityOfServiceLevel _publishingQualityOfService;
        private readonly MqttQualityOfServiceLevel _receivingQualityOfService;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerMqtt class using the specified fallback type.
        /// </summary>
        /// <param name="options">The options for the connection and messages sent/received on the connection.</param>
        internal ProvisioningTransportHandlerMqtt(ProvisioningClientOptions options)
        {
            _options = options;
            _settings = (ProvisioningClientMqttSettings)options.TransportSettings;

            _publishingQualityOfService = _settings.PublishToServerQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            _receivingQualityOfService = _settings.ReceivingQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="provisioningRequest">The provisioning request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        internal override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest provisioningRequest,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");

            Argument.AssertNotNull(provisioningRequest, nameof(provisioningRequest));

            cancellationToken.ThrowIfCancellationRequested();

            _isClosing = false;
            _isOpening = true;
            _connectionLossCause = null;
            using IMqttClient mqttClient = s_mqttFactory.CreateMqttClient();
            MqttClientOptionsBuilder mqttClientOptionsBuilder = CreateMqttClientOptions(provisioningRequest);
            mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessageAsync;

            // Link the user-supplied cancellation token with a cancellation token that is cancelled
            // when the connection is lost so that all operations stop when either the user
            // cancels the token or when the connection is lost.
            _connectionLostCancellationToken = new CancellationTokenSource();
            using CancellationTokenSource linkedCancellationToken =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _connectionLostCancellationToken.Token);

            // Additional context to be included in the error message thrown if the connection is lost to explain
            // when the connection was lost. Mostly for e2e test debugging, but users may find this helpful as well.
            string currentStatus = "opening MQTT connection";
            try
            {
                try
                {
                    MqttClientConnectResult connectResult = await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), cancellationToken).ConfigureAwait(false);
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"MQTT connect responded with status code '{connectResult.ResultCode}'");
                    mqttClient.DisconnectedAsync += HandleDisconnectionAsync;
                    _isOpening = false;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new DeviceProvisioningClientException("Failed to open the MQTT connection.", ex, true);
                }

                currentStatus = "subscribing to registration responses";
                await SubscribeToRegistrationResponseMessagesAsync(mqttClient, linkedCancellationToken.Token).ConfigureAwait(false);

                currentStatus = "publishing registration request";
                RegistrationOperationStatus registrationStatus = await PublishRegistrationRequestAsync(mqttClient, provisioningRequest, linkedCancellationToken.Token).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Successfully sent the initial registration request. Current status '{registrationStatus.Status}'. Now polling until provisioning has finished.");

                currentStatus = "polling for registration state";
                DeviceRegistrationResult registrationResult = await PollUntilProvisionigFinishesAsync(mqttClient, registrationStatus.OperationId, linkedCancellationToken.Token).ConfigureAwait(false);

                try
                {
                    currentStatus = "closing MQTT connection";
                    _isClosing = true;
                    mqttClient.DisconnectedAsync -= HandleDisconnectionAsync;
                    await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), cancellationToken);
                }
                catch (Exception ex)
                {
                    // Deliberately not rethrowing the exception because this is a "best effort" close.
                    // The service may not have acknowledged that the client closed the connection, but
                    // all local resources have been closed. The service will eventually realize the
                    // connection is closed in cases like these.

                    if (Logging.IsEnabled)
                        Logging.Error(this, $"Failed to gracefully close the MQTT client. {ex}");
                }

                return registrationResult;
            }
            catch (OperationCanceledException) when (_connectionLostCancellationToken.IsCancellationRequested)
            {
                // _connectionLostCancellationToken is cancelled when the connection is lost. This acts as
                // a signal to stop waiting on any service response and to throw the below exception up to the user
                // so they can retry.

                // Deliberately not including the caught exception as this exception's inner exception because
                // if the user sees an OperationCancelledException in the thrown exception, they may think they cancelled
                // the operation even though they didn't.
                throw new DeviceProvisioningClientException($"MQTT connection was lost while {currentStatus}.", _connectionLossCause, true);

                // If it was the user's cancellation token that requested cancellation, then this catch block
                // won't execute and the OperationCanceledException will be thrown as expected.
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new DeviceProvisioningClientException("Timed out while provisioning.", ex, true);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException("Timed out while provisioning.", ex);
            }
            finally
            {
                mqttClient.ApplicationMessageReceivedAsync -= HandleReceivedMessageAsync; // safe to -= this value more than once
                mqttClient.DisconnectedAsync -= HandleDisconnectionAsync; // safe to -= this value more than once
                _connectionLostCancellationToken?.Dispose();
            }
        }

        private async Task SubscribeToRegistrationResponseMessagesAsync(IMqttClient mqttClient, CancellationToken cancellationToken)
        {
            try
            {
                MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(SubscribeFilter, _receivingQualityOfService)
                    .Build();

                MqttClientSubscribeResult subscribeResults = await mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

                if (subscribeResults?.Items == null)
                {
                    throw new DeviceProvisioningClientException($"Failed to subscribe to topic '{SubscribeFilter}'.", true);
                }

                MqttClientSubscribeResultItem subscribeResult = subscribeResults.Items.FirstOrDefault();

                if (!subscribeResult.TopicFilter.Topic.Equals(SubscribeFilter))
                {
                    throw new DeviceProvisioningClientException($"Received unexpected subscription to topic '{subscribeResult.TopicFilter.Topic}'.", true);
                }
            }
            catch (Exception ex) when (ex is not DeviceProvisioningClientException && ex is not OperationCanceledException)
            {
                throw new DeviceProvisioningClientException("Failed to subscribe to the registrations response topic.", ex, true);
            }
        }

        private async Task<RegistrationOperationStatus> PublishRegistrationRequestAsync(IMqttClient mqttClient, ProvisioningTransportRegisterRequest provisioningRequest, CancellationToken cancellationToken)
        {
            byte[] payload = new byte[0];
            if (provisioningRequest.Payload != null)
            {
                var registrationRequest = new DeviceRegistration(new JRaw(provisioningRequest.Payload));
                string requestString = JsonConvert.SerializeObject(registrationRequest);
                payload = Encoding.UTF8.GetBytes(requestString);
            }

            string registrationTopic = string.Format(CultureInfo.InvariantCulture, RegisterTopic, ++_packetId);
            var message = new MqttApplicationMessageBuilder()
                .WithPayload(payload)
                .WithTopic(registrationTopic)
                .WithQualityOfServiceLevel(_publishingQualityOfService)
                .Build();

            _startProvisioningRequestStatusSource = new TaskCompletionSource<RegistrationOperationStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                MqttClientPublishResult publishResult = await mqttClient.PublishAsync(message, cancellationToken).ConfigureAwait(false);

                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new DeviceProvisioningClientException($"Failed to publish the MQTT packet for message with reason code '{publishResult.ReasonCode}'.", true);
                }
            }
            catch (Exception ex) when (ex is not DeviceProvisioningClientException && ex is not OperationCanceledException)
            {
                throw new DeviceProvisioningClientException("Failed to send the initial registration request.", ex, true);
            }

            if (Logging.IsEnabled)
                Logging.Info(this, "Published the initial registration request, now waiting for the service's response.");

            RegistrationOperationStatus registrationStatus = await GetTaskCompletionSourceResultAsync(
                    _startProvisioningRequestStatusSource,
                    "Timed out when sending the registration request.",
                    cancellationToken)
                .ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Info(this, $"Service responded to the initial registration request with status '{registrationStatus.Status}'.");

            if (registrationStatus.Status != RegistrationOperationStatus.OperationStatusAssigning)
            {
                throw new DeviceProvisioningClientException($"Failed to provision. Service responded with status {registrationStatus.Status}.", true);
            }

            return registrationStatus;
        }

        private async Task<DeviceRegistrationResult> PollUntilProvisionigFinishesAsync(IMqttClient mqttClient, string operationId, CancellationToken cancellationToken)
        {
            while (true)
            {
                string topicName = string.Format(CultureInfo.InvariantCulture, GetOperationsTopic, ++_packetId, operationId);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topicName)
                    .WithQualityOfServiceLevel(_publishingQualityOfService)
                    .Build();

                _checkRegistrationOperationStatusSource = new TaskCompletionSource<RegistrationOperationStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

                MqttClientPublishResult publishResult = await mqttClient.PublishAsync(message, cancellationToken).ConfigureAwait(false);

                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new DeviceProvisioningClientException($"Failed to publish the MQTT registration message with reason code '{publishResult.ReasonCode}'.", true);
                }

                RegistrationOperationStatus currentStatus = await GetTaskCompletionSourceResultAsync(
                        _checkRegistrationOperationStatusSource,
                        "Timed out while polling the registration status.",
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Current provisioning state: {currentStatus.RegistrationState.Status}.");

                if (currentStatus.RegistrationState.Status != ProvisioningRegistrationStatusType.Assigning)
                {
                    return currentStatus.RegistrationState;
                }

                // The service is expected to return a value signalling how long to wait before polling again, but
                // the SDK has a default value for when the service does not send that value. Included in this default value
                // is some jitter to help stagger the requests if multiple provisioning device clients are checking their provisioning
                // state at the same time.
                TimeSpan pollingDelay = currentStatus.RetryAfter ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPollingInterval);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Polling for the current state again in {pollingDelay}.");

                await Task.Delay(pollingDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        private MqttClientOptionsBuilder CreateMqttClientOptions(ProvisioningTransportRegisterRequest provisioningRequest)
        {
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            string hostName = provisioningRequest.GlobalDeviceEndpoint;

            if (_settings.Protocol == ProvisioningClientTransportProtocol.Tcp)
            {
                // "ssl://" prefix is not needed here because the MQTT library adds it for us.
                var uri = hostName;
                mqttClientOptionsBuilder.WithTcpServer(uri, MqttTcpPort);
            }
            else
            {
                var uri = $"wss://{hostName}";
                mqttClientOptionsBuilder.WithWebSocketServer(uri);

                if (_settings.Proxy != null)
                {
                    Uri serviceUri = new(uri);
                    Uri proxyUri = _settings.Proxy.GetProxy(serviceUri);

                    if (_settings.Proxy.Credentials != null)
                    {
                        NetworkCredential credentials = _settings.Proxy.Credentials.GetCredential(serviceUri, BasicProxyAuthentication);
                        string proxyUsername = credentials.UserName;
                        string proxyPassword = credentials.Password;
                        mqttClientOptionsBuilder.WithProxy(proxyUri.AbsoluteUri, proxyUsername, proxyPassword);
                    }
                    else
                    {
                        mqttClientOptionsBuilder.WithProxy(proxyUri.AbsoluteUri);
                    }
                }
            }

            MqttClientOptionsBuilderTlsParameters tlsParameters = new MqttClientOptionsBuilderTlsParameters();
            var password = "";
            if (provisioningRequest.Authentication is AuthenticationProviderX509 x509Auth)
            {
                var certs = new List<X509Certificate>()
                {
                    x509Auth.GetAuthenticationCertificate()
                };

                tlsParameters.Certificates = certs;
            }
            else if (provisioningRequest.Authentication is AuthenticationProviderSymmetricKey key1)
            {
                password = ProvisioningSasBuilder.BuildSasSignature(key1.GetPrimaryKey(), string.Concat(provisioningRequest.IdScope, '/', "registrations", '/', provisioningRequest.Authentication.GetRegistrationId()), TimeSpan.FromHours(1));
            }
            else if (provisioningRequest.Authentication is AuthenticationProviderTpm)
            {
                throw new NotSupportedException("TPM authentication is not supported over MQTT TCP or MQTT web socket.");
            }

            var username = string.Format(
                    CultureInfo.InvariantCulture,
                    UsernameFormat,
                    provisioningRequest.IdScope,
                    provisioningRequest.Authentication.GetRegistrationId(),
                    ClientApiVersionHelper.ApiVersion,
                    Uri.EscapeDataString(_options.UserAgentInfo.ToString()));

            mqttClientOptionsBuilder
                .WithClientId(provisioningRequest.Authentication.GetRegistrationId())
                .WithCredentials(username, password);

            if (_settings.RemoteCertificateValidationCallback != null)
            {
                tlsParameters.CertificateValidationHandler = CertificateValidationHandler;
            }

            tlsParameters.UseTls = true;
            tlsParameters.SslProtocol = _settings.SslProtocols;
            mqttClientOptionsBuilder
                .WithTls(tlsParameters)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311) // 3.1.1
                .WithKeepAlivePeriod(_settings.IdleTimeout)
                .WithTimeout(TimeSpan.FromMilliseconds(-1)); // MQTTNet will only time out if the cancellation token requests cancellation.

            return mqttClientOptionsBuilder;
        }

        private bool CertificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            return _settings.RemoteCertificateValidationCallback.Invoke(
                new object(), //TODO Tim to check with Abhipsa about this and if it is necessary
                args.Certificate,
                args.Chain,
                args.SslPolicyErrors);
        }

        private Task HandleReceivedMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;
            string topic = receivedEventArgs.ApplicationMessage.Topic;

            if (Logging.IsEnabled)
                Logging.Info(this, $"Received MQTT message from service with topic '{topic}'.");

            if (!_startProvisioningRequestStatusSource.Task.IsCompleted)
            {
                // The initial provisioning request's response topic is shaped like "$dps/registrations/res/202/?$rid=1&retry-after=3"
                string jsonString = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
                RegistrationOperationStatus operation = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonString);
                _startProvisioningRequestStatusSource.TrySetResult(operation);
            }
            else
            {
                // All status polling requests' response topics are shaped like "$dps/registrations/res/200/?$rid=2"
                string jsonString = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
                RegistrationOperationStatus operation = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonString);

                operation.RetryAfter = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(topic, s_defaultOperationPollingInterval);

                _checkRegistrationOperationStatusSource.TrySetResult(operation);
            }

            return Task.CompletedTask;
        }

        private Task HandleDisconnectionAsync(MqttClientDisconnectedEventArgs disconnectedEventArgs)
        {
            _connectionLossCause = disconnectedEventArgs.Exception;

            if (Logging.IsEnabled)
                Logging.Error(this, $"MQTT connection was lost '{_connectionLossCause}'.");

            // If it was an unexpected disconnect. Ignore cases when the user intentionally closes the connection.
            if (disconnectedEventArgs.ClientWasConnected && !_isClosing && !_isOpening)
            {
                _connectionLostCancellationToken.Cancel();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the result of the provided task completion source or throws OperationCanceledException if the provided
        /// cancellation token is cancelled beforehand.
        /// </summary>
        /// <typeparam name="T">The type of the result of the task completion source.</typeparam>
        /// <param name="taskCompletionSource">The task completion source to asynchronously wait for the result of.</param>
        /// <param name="timeoutErrorMessage">The error message to put in the OperationCanceledException if this taks times out.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the provided task completion source if it completes before the provided cancellation token is cancelled.</returns>
        /// <exception cref="OperationCanceledException">If the cancellation token is cancelled before the provided task completion source finishes.</exception>
        private static async Task<T> GetTaskCompletionSourceResultAsync<T>(
            TaskCompletionSource<T> taskCompletionSource,
            string timeoutErrorMessage,
            CancellationToken cancellationToken)
        {
            // Note that Task.Delay(-1, cancellationToken) effectively waits until the cancellation token is cancelled. The -1 value
            // just means that the task is allowed to run indefinitely.
            Task finishedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);

            // If the finished task is not the cancellation token
            if (finishedTask == taskCompletionSource.Task)
            {
                return await ((Task<T>)finishedTask).ConfigureAwait(false);
            }

            // Otherwise throw operation cancelled exception since the cancellation token was cancelled before the task finished.
            throw new OperationCanceledException(timeoutErrorMessage);
        }
    }
}
