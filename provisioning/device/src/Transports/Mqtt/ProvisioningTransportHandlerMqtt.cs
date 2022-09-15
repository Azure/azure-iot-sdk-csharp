// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Provisioning.Client.Transports.Mqtt;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the MQTT protocol implementation for the provisioning transport handler.
    /// </summary>
    public class ProvisioningTransportHandlerMqtt : ProvisioningTransportHandler
    {
        private const int MaxMessageSize = 256 * 1024;
        private const int MqttTcpPort = 8883;
        private const string WsScheme = "wss";
        private const string UsernameFormat = "{0}/registrations/{1}/api-version={2}&ClientVersion={3}";
        private const string SubscribeFilter = "$dps/registrations/res/#";
        private const string RegisterTopic = "$dps/registrations/PUT/iotdps-register/?$rid={0}";
        private const string GetOperationsTopic = "$dps/registrations/GET/iotdps-get-operationstatus/?$rid={0}&operationId={1}";
        private const string Registration = "registration";

        private static readonly Regex s_registrationStatusTopicRegex = new Regex("^\\$dps/registrations/res/(.*?)/\\?\\$rid=(.*?)$", RegexOptions.Compiled);
        private static readonly TimeSpan s_defaultOperationPollingInterval = TimeSpan.FromSeconds(2);
        private static readonly MqttQualityOfServiceLevel QoS = MqttQualityOfServiceLevel.AtLeastOnce;
        private static readonly MqttFactory s_mqttFactory = new MqttFactory(new MqttLogger());

        private TaskCompletionSource<RegistrationOperationStatus> _startProvisioningRequestStatusSource;
        private TaskCompletionSource<RegistrationOperationStatus> _checkRegistrationOperationStatusSource;
        private int _packetId;
        private CancellationTokenSource _connectionLostCancellationToken;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerMqtt class using the specified fallback type.
        /// </summary>
        /// <param name="transportProtocol">The protocol over which the MQTT transport communicates (i.e., TCP or web socket).</param>
        public ProvisioningTransportHandlerMqtt(
            ProvisioningClientTransportProtocol transportProtocol = ProvisioningClientTransportProtocol.Tcp)
        {
            TransportProtocol = transportProtocol;
        }

        /// <summary>
        /// The protocol over which the MQTT transport communicates (i.e., TCP or web socket).
        /// </summary>
        public ProvisioningClientTransportProtocol TransportProtocol { get; private set; }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="provisioningRequest">The provisioning request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest provisioningRequest,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");

            Argument.AssertNotNull(provisioningRequest, nameof(provisioningRequest));

            cancellationToken.ThrowIfCancellationRequested();

            using IMqttClient mqttClient = s_mqttFactory.CreateMqttClient();
            MqttClientOptionsBuilder mqttClientOptionsBuilder = CreateMqttClientOptions(provisioningRequest);
            mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            mqttClient.DisconnectedAsync += HandleDisconnection;

            _connectionLostCancellationToken = new CancellationTokenSource();
            using CancellationTokenSource linkedCancellationToken =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _connectionLostCancellationToken.Token);

            try
            {
                try
                {
                    await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), linkedCancellationToken.Token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new ProvisioningTransportException("Failed to open the MQTT connection", e);
                }

                await SubscribeToRegistrationResponseMessagesAsync(mqttClient, linkedCancellationToken.Token).ConfigureAwait(false);

                RegistrationOperationStatus registrationStatus = await PublishRegistrationRequest(mqttClient, provisioningRequest, linkedCancellationToken.Token).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, "Successfully sent the initial registration request, now polling until provisioning has finished.");

                return await PollUntilProvisionigFinishes(mqttClient, registrationStatus.OperationId, linkedCancellationToken.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // _connectionLostCancellationToken is cancelled when the connection is lost. This acts as
                // a signal to stop waiting on any service response and to throw the below exception up to the user
                // so they can retry.
                if (_connectionLostCancellationToken.IsCancellationRequested)
                {
                    // Deliberately not including the caught exception as this exception's inner exception because
                    // if the user sees an OperationCancelledException in the thrown exception, they may think they cancelled
                    // the operation even though they didn't.
                    throw new ProvisioningTransportException("MQTT connection was lost during provisioning.", true);
                }

                // If it was the user's cancellation token that requested cancellation, then just rethrow
                // the exception as is.
                throw;
            }
        }

        private async Task<RegistrationOperationStatus> PublishRegistrationRequest(IMqttClient mqttClient, ProvisioningTransportRegisterRequest provisioningRequest, CancellationToken cancellationToken)
        {
            DeviceRegistration registrationRequest = new DeviceRegistration()
            {
                Payload = new JRaw(provisioningRequest.Payload),
            };

            byte[] payload = new byte[0];

            if (provisioningRequest.Payload != null)
            {
                string requestString = JsonConvert.SerializeObject(registrationRequest);
                payload = Encoding.UTF8.GetBytes(requestString);
            }

            int packetId = GetNextPacketId();
            string registrationTopic = string.Format(CultureInfo.InvariantCulture, RegisterTopic, packetId);
            var message = new MqttApplicationMessageBuilder()
                .WithPayload(payload)
                .WithTopic(registrationTopic)
                .Build();

            _startProvisioningRequestStatusSource = new TaskCompletionSource<RegistrationOperationStatus>();

            try
            {
                MqttClientPublishResult publishResult = await mqttClient.PublishAsync(message, cancellationToken).ConfigureAwait(false);

                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new ProvisioningTransportException($"Failed to publish the MQTT packet for message with reason code {publishResult.ReasonCode}", true);
                }
            }
            catch (Exception e) when (e is not ProvisioningTransportException)
            {
                throw new ProvisioningTransportException("Failed to send the initial registration request", e, true);
            }

            //TODO cancellation token?
            RegistrationOperationStatus registrationStatus = await _startProvisioningRequestStatusSource.Task.ConfigureAwait(false);

            if (registrationStatus.Status != RegistrationOperationStatus.OperationStatusAssigning)
            {
                throw new ProvisioningTransportException($"Failed to start provisioning. Service responded with status {registrationStatus.Status}", true);
            }

            return registrationStatus;
        }

        private async Task SubscribeToRegistrationResponseMessagesAsync(IMqttClient mqttClient, CancellationToken cancellationToken)
        {
            try
            {
                MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(SubscribeFilter, QoS)
                .Build();

                MqttClientSubscribeResult subscribeResults = await mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

                if (subscribeResults == null || subscribeResults.Items == null)
                {
                    throw new ProvisioningTransportException("Failed to subscribe to topic " + SubscribeFilter, true);
                }

                MqttClientSubscribeResultItem subscribeResult = subscribeResults.Items.FirstOrDefault();

                if (!subscribeResult.TopicFilter.Topic.Equals(SubscribeFilter))
                {
                    throw new ProvisioningTransportException("Received unexpected subscription to topic " + subscribeResult.TopicFilter.Topic, true);
                }
            }
            catch (Exception e) when (e is not ProvisioningTransportException)
            {
                throw new ProvisioningTransportException("Failed to subscribe to the registrations response topic", e, true);
            }
        }

        private MqttClientOptionsBuilder CreateMqttClientOptions(ProvisioningTransportRegisterRequest provisioningRequest)
        {
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            string hostName = provisioningRequest.GlobalDeviceEndpoint;

            if (TransportProtocol == ProvisioningClientTransportProtocol.WebSocket)
            {
                var uri = "wss://" + hostName;
                mqttClientOptionsBuilder.WithWebSocketServer(uri);

                if (Proxy != null)
                {
                    var serviceUri = new Uri(uri);
                    var proxyUri = Proxy.GetProxy(serviceUri);

                    if (Proxy.Credentials != null)
                    {
                        NetworkCredential credentials = Proxy.Credentials.GetCredential(serviceUri, "Basic");
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
            else
            {
                // "ssl://" prefix is not needed here
                var uri = hostName;
                mqttClientOptionsBuilder.WithTcpServer(uri, MqttTcpPort);
            }

            MqttClientOptionsBuilderTlsParameters tlsParameters = new MqttClientOptionsBuilderTlsParameters();

            string password = "";
            if (provisioningRequest.Authentication is AuthenticationProviderX509 x509Auth)
            {
                List<X509Certificate> certs = new()
                {
                    x509Auth.GetAuthenticationCertificate()
                };

                tlsParameters.Certificates = certs;
            }
            else if (provisioningRequest.Authentication is AuthenticationProviderSymmetricKey key1)
            {
                password = ProvisioningSasBuilder.BuildSasSignature(Registration, key1.GetPrimaryKey(), string.Concat(provisioningRequest.IdScope, '/', "registrations", '/', provisioningRequest.Authentication.GetRegistrationId()), TimeSpan.FromHours(1));
            }
            else if (provisioningRequest.Authentication is AuthenticationProviderTpm)
            {
                throw new NotSupportedException("TPM authentication is not supported over MQTT or MQTT websockets");
            }

            string username = string.Format(
                    CultureInfo.InvariantCulture,
                    UsernameFormat,
                    provisioningRequest.IdScope,
                    provisioningRequest.Authentication.GetRegistrationId(),
                    ClientApiVersionHelper.ApiVersion,
                    Uri.EscapeDataString(provisioningRequest.ProductInfo));

            mqttClientOptionsBuilder.WithClientId(provisioningRequest.Authentication.GetRegistrationId());
            mqttClientOptionsBuilder.WithCredentials(username, password);

            if (RemoteCertificateValidationCallback != null)
            {
                tlsParameters.CertificateValidationHandler = certificateValidationHandler;
            }

            tlsParameters.UseTls = true;
            tlsParameters.SslProtocol = SslProtocols;
            mqttClientOptionsBuilder.WithTls(tlsParameters);
            mqttClientOptionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311); // 3.1.1
            mqttClientOptionsBuilder.WithKeepAlivePeriod(IdleTimeout);

            return mqttClientOptionsBuilder;
        }

        private bool certificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            return RemoteCertificateValidationCallback.Invoke(
                new object(), //TODO
                args.Certificate,
                args.Chain,
                args.SslPolicyErrors);
        }

        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;
            string topic = receivedEventArgs.ApplicationMessage.Topic;

            if (!_startProvisioningRequestStatusSource.Task.IsCompleted)
            {
                // The initial provisioning request's response topic is shaped like "$dps/registrations/res/202/?$rid=1&retry-after=3"
                string jsonString = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
                RegistrationOperationStatus operation = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonString);
                _startProvisioningRequestStatusSource.SetResult(operation);
            }
            else
            {
                // All status polling requests' response topics are shaped like "$dps/registrations/res/200/?$rid=2"
                string jsonString = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
                RegistrationOperationStatus operation = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonString);

                operation.RetryAfter = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(topic, s_defaultOperationPollingInterval);

                _checkRegistrationOperationStatusSource.SetResult(operation);
            }

            return Task.CompletedTask;
        }

        private Task HandleDisconnection(MqttClientDisconnectedEventArgs disconnectedEventArgs)
        {
            _connectionLostCancellationToken.Cancel();
            return Task.CompletedTask;
        }

        private async Task<DeviceRegistrationResult> PollUntilProvisionigFinishes(IMqttClient mqttClient, string operationId, CancellationToken cancellationToken)
        {
            while (true)
            {
                int packetId = GetNextPacketId();
                string topicName = string.Format(CultureInfo.InvariantCulture, GetOperationsTopic, packetId, operationId);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topicName)
                    .Build();

                _checkRegistrationOperationStatusSource = new TaskCompletionSource<RegistrationOperationStatus>();

                MqttClientPublishResult publishResult = await mqttClient.PublishAsync(message, cancellationToken).ConfigureAwait(false);

                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new ProvisioningTransportException($"Failed to publish the MQTT registration message with reason code {publishResult.ReasonCode}", true);
                }

                //TODO cancellation token?
                RegistrationOperationStatus currentStatus = await _checkRegistrationOperationStatusSource.Task.ConfigureAwait(false);

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
                    Logging.Info(this, $"Polling for the current state again in {pollingDelay.TotalMilliseconds} milliseconds.");

                await Task.Delay(pollingDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        private ushort GetNextPacketId()
        {
            unchecked
            {
                ushort newIdShort;
                int newId = Interlocked.Increment(ref _packetId);

                newIdShort = (ushort)newId;
                return newIdShort == 0 ? GetNextPacketId() : newIdShort;
            }
        }

        private ushort GetCurrentPacketId()
        {
            unchecked
            {
                return (ushort)Volatile.Read(ref _packetId);
            }
        }
    }
}
