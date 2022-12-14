// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class MqttTransportHandler : TransportHandler, IDisposable
    {
        private const int ProtocolGatewayPort = 8883;

        private const string DeviceToCloudMessagesTopicFormat = "devices/{0}/messages/events/";
        private const string ModuleToCloudMessagesTopicFormat = "devices/{0}/modules/{1}/messages/events/";
        private readonly string _deviceToCloudMessagesTopic;
        private readonly string _moduleToCloudMessagesTopic;

        // Topic names for receiving cloud-to-device messages.

        private const string DeviceBoundMessagesTopicFormat = "devices/{0}/messages/devicebound/";
        private readonly string _deviceBoundMessagesTopic;

        // Topic names for enabling input events on edge Modules.

        private const string EdgeModuleInputEventsTopicFormat = "devices/{0}/modules/{1}/inputs/";
        private readonly string _edgeModuleInputEventsTopic;

        // Topic names for enabling events on non-edge Modules.

        private const string ModuleEventMessageTopicFormat = "devices/{0}/modules/{1}/";
        private readonly string _moduleEventMessageTopic;

        // Topic names for retrieving a device's twin properties.
        // The client first subscribes to "$iothub/twin/res/#", to receive the operation's responses.
        // It then sends an empty message to the topic "$iothub/twin/GET/?$rid={request id}, with a populated value for request Id.
        // The service then sends a response message containing the device twin data on topic "$iothub/twin/res/{status}/?$rid={request id}", using the same request Id as the request.

        private const string TwinResponseTopic = "$iothub/twin/res/";
        private const string TwinGetTopicFormat = "$iothub/twin/GET/?$rid={0}";
        private const string TwinResponseTopicPattern = @"\$iothub/twin/res/(\d+)/(\?.+)";
        private readonly Regex _twinResponseTopicRegex = new(TwinResponseTopicPattern, RegexOptions.Compiled);

        // Topic name for updating device twin's reported properties.
        // The client first subscribes to "$iothub/twin/res/#", to receive the operation's responses.
        // The client then sends a message containing the twin update to "$iothub/twin/PATCH/properties/reported/?$rid={request id}", with a populated value for request Id.
        // The service then sends a response message containing the new ETag value for the reported properties collection on the topic "$iothub/twin/res/{status}/?$rid={request id}", using the same request Id as the request.
        private const string TwinReportedPropertiesPatchTopicFormat = "$iothub/twin/PATCH/properties/reported/?$rid={0}";

        // Topic names for receiving twin desired property update notifications.

        private const string TwinDesiredPropertiesPatchTopic = "$iothub/twin/PATCH/properties/desired/";

        // Topic name for responding to direct methods.
        // The client first subscribes to "$iothub/methods/POST/#".
        // The service sends method requests to the topic "$iothub/methods/POST/{method name}/?$rid={request id}".
        // The client responds to the direct method invocation by sending a message to the topic "$iothub/methods/res/{status}/?$rid={request id}", using the same request Id as the request.

        private const string DirectMethodsRequestTopic = "$iothub/methods/POST/";
        private const string DirectMethodsResponseTopicFormat = "$iothub/methods/res/{0}/?$rid={1}";

        private const string WildCardTopicFilter = "#";
        private const string RequestIdTopicKey = "$rid";
        private const string VersionTopicKey = "$version";

        private const string BasicProxyAuthentication = "Basic";

        private const string ConnectTimedOutErrorMessage = "Timed out waiting for MQTT connection to open.";
        private const string MessageTimedOutErrorMessage = "Timed out waiting for MQTT message to be acknowledged.";
        private const string SubscriptionTimedOutErrorMessage = "Timed out waiting for MQTT subscription to be acknowledged.";
        private const string UnsubscriptionTimedOutErrorMessage = "Timed out waiting for MQTT unsubscription to be acknowledged.";

        private readonly MqttClientOptionsBuilder _mqttClientOptionsBuilder;

        private readonly MqttQualityOfServiceLevel _publishingQualityOfService;
        private readonly MqttQualityOfServiceLevel _receivingQualityOfService;

        private readonly Func<DirectMethodRequest, Task> _methodListener;
        private readonly Action<DesiredProperties> _onDesiredStatePatchListener;
        private readonly Func<IncomingMessage, Task<MessageAcknowledgement>> _messageReceivedListener;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<GetTwinResponse>> _getTwinResponseCompletions = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<PatchTwinResponse>> _reportedPropertyUpdateResponseCompletions = new();

        private readonly ConcurrentDictionary<string, DateTimeOffset> _twinResponseTimeouts = new();

        private bool _isSubscribedToTwinResponses;

        // Timer to check if any expired messages exist. The timer is executed after each hour of execution.
        private readonly Timer _twinTimeoutTimer;

        private static TimeSpan s_twinResponseTimeout = TimeSpan.FromMinutes(60);

        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly string _hostName;
        private readonly string _modelId;
        private readonly PayloadConvention _payloadConvention;
        private readonly ProductInfo _productInfo;
        private readonly IotHubClientMqttSettings _mqttTransportSettings;
        private readonly IotHubConnectionCredentials _connectionCredentials;

        private static readonly Dictionary<string, string> s_toSystemPropertiesMap = new()
        {
            { IotHubWirePropertyNames.AbsoluteExpiryTime, MessageSystemPropertyNames.ExpiryTimeUtc },
            { IotHubWirePropertyNames.CorrelationId, MessageSystemPropertyNames.CorrelationId },
            { IotHubWirePropertyNames.MessageId, MessageSystemPropertyNames.MessageId },
            { IotHubWirePropertyNames.To, MessageSystemPropertyNames.To },
            { IotHubWirePropertyNames.UserId, MessageSystemPropertyNames.UserId },
            { IotHubWirePropertyNames.MessageSchema, MessageSystemPropertyNames.MessageSchema },
            { IotHubWirePropertyNames.CreationTimeUtc, MessageSystemPropertyNames.CreationTimeUtc },
            { IotHubWirePropertyNames.ContentType, MessageSystemPropertyNames.ContentType },
            { IotHubWirePropertyNames.ContentEncoding, MessageSystemPropertyNames.ContentEncoding },
            { MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation },
            { IotHubWirePropertyNames.ConnectionDeviceId, MessageSystemPropertyNames.ConnectionDeviceId },
            { IotHubWirePropertyNames.ConnectionModuleId, MessageSystemPropertyNames.ConnectionModuleId },
            { IotHubWirePropertyNames.MqttDiagIdKey, MessageSystemPropertyNames.DiagId },
            { IotHubWirePropertyNames.MqttDiagCorrelationContextKey, MessageSystemPropertyNames.DiagCorrelationContext },
            { IotHubWirePropertyNames.InterfaceId, MessageSystemPropertyNames.InterfaceId },
        };

        private static readonly Dictionary<string, string> s_fromSystemPropertiesMap = new()
        {
            { MessageSystemPropertyNames.ExpiryTimeUtc, IotHubWirePropertyNames.AbsoluteExpiryTime },
            { MessageSystemPropertyNames.CorrelationId, IotHubWirePropertyNames.CorrelationId },
            { MessageSystemPropertyNames.MessageId, IotHubWirePropertyNames.MessageId },
            { MessageSystemPropertyNames.To, IotHubWirePropertyNames.To },
            { MessageSystemPropertyNames.UserId, IotHubWirePropertyNames.UserId },
            { MessageSystemPropertyNames.MessageSchema, IotHubWirePropertyNames.MessageSchema },
            { MessageSystemPropertyNames.CreationTimeUtc, IotHubWirePropertyNames.CreationTimeUtc },
            { MessageSystemPropertyNames.ContentType, IotHubWirePropertyNames.ContentType },
            { MessageSystemPropertyNames.ContentEncoding, IotHubWirePropertyNames.ContentEncoding },
            { MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation },
            { MessageSystemPropertyNames.OutputName, IotHubWirePropertyNames.OutputName },
            { MessageSystemPropertyNames.DiagId, IotHubWirePropertyNames.MqttDiagIdKey },
            { MessageSystemPropertyNames.DiagCorrelationContext, IotHubWirePropertyNames.MqttDiagCorrelationContextKey },
            { MessageSystemPropertyNames.InterfaceId, IotHubWirePropertyNames.InterfaceId },
            { MessageSystemPropertyNames.ComponentName,IotHubWirePropertyNames.ComponentName },
        };

        internal IMqttClient _mqttClient;

        private MqttClientOptions _mqttClientOptions;

        internal MqttTransportHandler(PipelineContext context, IotHubClientMqttSettings settings)
            : base(context, settings)
        {
            _mqttTransportSettings = settings;
            _deviceId = context.IotHubConnectionCredentials.DeviceId;
            _moduleId = context.IotHubConnectionCredentials.ModuleId;

            _methodListener = context.MethodCallback;
            _messageReceivedListener = context.MessageEventCallback;
            _onDesiredStatePatchListener = context.DesiredPropertyUpdateCallback;

            _deviceToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceToCloudMessagesTopicFormat, _deviceId);
            _moduleToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, ModuleToCloudMessagesTopicFormat, _deviceId, _moduleId);
            _deviceBoundMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicFormat, _deviceId);
            _moduleEventMessageTopic = string.Format(CultureInfo.InvariantCulture, ModuleEventMessageTopicFormat, _deviceId, _moduleId);
            _edgeModuleInputEventsTopic = string.Format(CultureInfo.InvariantCulture, EdgeModuleInputEventsTopicFormat, _deviceId, _moduleId);

            _modelId = context.ModelId;
            _productInfo = context.ProductInfo;
            _payloadConvention = context.PayloadConvention;

            var mqttFactory = new MqttFactory(new MqttLogger());
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            _hostName = context.IotHubConnectionCredentials.HostName;
            _connectionCredentials = context.IotHubConnectionCredentials;

            if (_mqttTransportSettings.Protocol == IotHubClientTransportProtocol.Tcp)
            {
                // "ssl://" prefix is not needed here because the MQTT library adds it for us.
                _mqttClientOptionsBuilder.WithTcpServer(_hostName, ProtocolGatewayPort);
            }
            else
            {
                string uri = $"wss://{_hostName}/$iothub/websocket";
                _mqttClientOptionsBuilder.WithWebSocketServer(uri);

                IWebProxy proxy = _transportSettings.Proxy;
                if (proxy != null)
                {
                    Uri serviceUri = new(uri);
                    Uri proxyUri = _transportSettings.Proxy.GetProxy(serviceUri);

                    if (proxy.Credentials != null)
                    {
                        NetworkCredential credentials = proxy.Credentials.GetCredential(serviceUri, BasicProxyAuthentication);
                        string username = credentials.UserName;
                        string password = credentials.Password;
                        _mqttClientOptionsBuilder.WithProxy(proxyUri.AbsoluteUri, username, password);
                    }
                    else
                    {
                        _mqttClientOptionsBuilder.WithProxy(proxyUri.AbsoluteUri);
                    }
                }
            }

            var tlsParameters = new MqttClientOptionsBuilderTlsParameters();
            List<X509Certificate> certs = _connectionCredentials.ClientCertificate == null
                ? new List<X509Certificate>(0)
                : new List<X509Certificate> { _connectionCredentials.ClientCertificate };

            tlsParameters.Certificates = certs;
            tlsParameters.IgnoreCertificateRevocationErrors = !settings.CertificateRevocationCheck;

            if (_mqttTransportSettings?.RemoteCertificateValidationCallback != null)
            {
                tlsParameters.CertificateValidationHandler = CertificateValidationHandler;
            }

            tlsParameters.UseTls = true;
            tlsParameters.SslProtocol = _mqttTransportSettings.SslProtocols;
            _mqttClientOptionsBuilder
                .WithTls(tlsParameters)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311) // 3.1.1
                .WithCleanSession(_mqttTransportSettings.CleanSession)
                .WithKeepAlivePeriod(_mqttTransportSettings.IdleTimeout)
                .WithTimeout(TimeSpan.FromMilliseconds(-1)); // MQTTNet will only time out if the cancellation token requests cancellation.

            if (_mqttTransportSettings.WillMessage != null)
            {
                _mqttClientOptionsBuilder
                    .WithWillTopic(_deviceToCloudMessagesTopic)
                    .WithWillPayload(_mqttTransportSettings.WillMessage.Payload);

                if (_mqttTransportSettings.WillMessage.QualityOfService == QualityOfService.AtMostOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
                }
                else if (_mqttTransportSettings.WillMessage.QualityOfService == QualityOfService.AtLeastOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
                }
            }

            _publishingQualityOfService = _mqttTransportSettings.PublishToServerQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            _receivingQualityOfService = _mqttTransportSettings.ReceivingQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            // Create a timer to remove any expired messages.
            _twinTimeoutTimer = new Timer(RemoveOldOperations);
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            string clientId = string.IsNullOrWhiteSpace(_moduleId) ? _deviceId : $"{_deviceId}/{_moduleId}";
            _mqttClientOptionsBuilder.WithClientId(clientId);

            string username = $"{_hostName}/{clientId}/?{ClientApiVersionHelper.ApiVersionQueryStringLatest}&DeviceClientType={Uri.EscapeDataString(_productInfo.ToString())}";

            if (!string.IsNullOrWhiteSpace(_modelId))
            {
                username += $"&model-id={Uri.EscapeDataString(_modelId)}";
            }

            if (!string.IsNullOrWhiteSpace(_mqttTransportSettings?.AuthenticationChain))
            {
                username += $"&auth-chain={Uri.EscapeDataString(_mqttTransportSettings.AuthenticationChain)}";
            }

            if (_connectionCredentials.SasTokenRefresher != null)
            {
                // Symmetric key authenticated connections need to set client Id, username, and password
                string password = await _connectionCredentials.SasTokenRefresher.GetTokenAsync(_connectionCredentials.IotHubHostName).ConfigureAwait(false);
                _mqttClientOptionsBuilder.WithCredentials(username, password);
            }
            else if (_connectionCredentials.SharedAccessSignature != null)
            {
                // Symmetric key authenticated connections need to set client Id, username, and password
                string password = _connectionCredentials.SharedAccessSignature;
                _mqttClientOptionsBuilder.WithCredentials(username, password);
            }
            else
            {
                // x509 authenticated connections only need to set client Id and username
                _mqttClientOptionsBuilder.WithCredentials(username);
            }

            _mqttClientOptions = _mqttClientOptionsBuilder.Build();

            try
            {
                await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken).ConfigureAwait(false);
                _mqttClient.DisconnectedAsync += HandleDisconnectionAsync;
                _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessageAsync;

                // The timer would invoke callback after every hour.
                _twinTimeoutTimer.Change(s_twinResponseTimeout, s_twinResponseTimeout);
            }
            catch (MqttConnectingFailedException ex)
            {
                MqttClientConnectResultCode connectCode = ex.ResultCode;
                switch (connectCode)
                {
                    case MqttClientConnectResultCode.BadUserNameOrPassword:
                    case MqttClientConnectResultCode.NotAuthorized:
                    case MqttClientConnectResultCode.ClientIdentifierNotValid:
                        throw new IotHubClientException(
                            "Failed to open the MQTT connection due to incorrect or unauthorized credentials",
                            IotHubClientErrorCode.Unauthorized, 
                            ex);
                    case MqttClientConnectResultCode.UnsupportedProtocolVersion:
                        // Should never happen since the protocol version (3.1.1) is hardcoded
                        throw new IotHubClientException(
                            "Failed to open the MQTT connection due to an unsupported MQTT version",
                            innerException: ex);
                    case MqttClientConnectResultCode.ServerUnavailable:
                        throw new IotHubClientException(
                            "MQTT connection rejected because the server was unavailable",
                            IotHubClientErrorCode.ServerBusy,
                            ex);
                    default:
                        if (ex.InnerException is MqttCommunicationTimedOutException)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                                // when the cancellation token requests cancellation.
                                throw new OperationCanceledException(ConnectTimedOutErrorMessage, ex);
                            }

                            // This execption may be thrown even if cancellation has not been requested yet.
                            // This case is treated as a timeout error rather than an OperationCanceledException
                            throw new IotHubClientException(
                                ConnectTimedOutErrorMessage,
                                IotHubClientErrorCode.Timeout,
                                ex);
                        }

                        // MQTT 3.1.1 only supports the above connect return codes, so this default case
                        // should never happen. For more details, see the MQTT 3.1.1 specification section "3.2.2.3 Connect Return code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html
                        // MQTT 5 supports a larger set of connect codes. See the MQTT 5.0 specification section "3.2.2.2 Connect Reason Code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901074
                        throw new IotHubClientException("Failed to open the MQTT connection", innerException: ex);
                }
            }
            catch (MqttCommunicationTimedOutException ex)
            {
                throw new IotHubClientException(
                    ConnectTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
        }

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Topic depends on if the client is a module client or a device client
                string baseTopicName = _moduleId == null ? _deviceToCloudMessagesTopic : _moduleToCloudMessagesTopic;
                string topicName = PopulateMessagePropertiesFromMessage(baseTopicName, message);

                MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topicName)
                    .WithPayload(message.GetPayloadObjectBytes())
                    .WithQualityOfServiceLevel(_publishingQualityOfService)
                    .Build();

                try
                {
                    MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        throw new IotHubClientException(
                            $"Failed to publish the MQTT packet for message with correlation Id {message.CorrelationId} with reason code {result.ReasonCode}",
                            IotHubClientErrorCode.NetworkErrors);
                    }
                }
                catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // This execption may be thrown even if cancellation has not been requested yet.
                    // This case is treated as a timeout error rather than an OperationCanceledException
                    throw new IotHubClientException(
                        MessageTimedOutErrorMessage,
                        IotHubClientErrorCode.Timeout,
                        ex);
                }
                catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
                {
                    // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                    // when the cancellation token requests cancellation.
                    throw new OperationCanceledException(MessageTimedOutErrorMessage, ex);
                }
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not InvalidOperationException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    $"Failed to send message with message Id: {message.MessageId}.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override Task SendTelemetryBatchAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("This operation is not supported over MQTT. Please refer to the API comments for additional details.");
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await SubscribeAsync(DirectMethodsRequestTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to enable receiving direct methods.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UnsubscribeAsync(DirectMethodsRequestTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to disable receiving direct methods.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            string topic = DirectMethodsResponseTopicFormat.FormatInvariant(methodResponse.Status, methodResponse.RequestId);
            byte[] serializedPayload = methodResponse.GetPayloadObjectBytes();
            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(serializedPayload)
                .WithQualityOfServiceLevel(_publishingQualityOfService)
                .Build();

            try
            {
                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException(
                        $"Failed to send direct method response with reason code {result.ReasonCode}",
                        IotHubClientErrorCode.NetworkErrors);
                }
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This execption may be thrown even if cancellation has not been requested yet.
                // This case is treated as a timeout error rather than an OperationCanceledException
                throw new IotHubClientException(
                    MessageTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException(MessageTimedOutErrorMessage, ex);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to send direct method response.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionCredentials.ModuleId))
                {
                    await SubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (_connectionCredentials.IsEdgeModule)
                    {
                        await SubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await SubscribeAsync(_moduleEventMessageTopic, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to enable receiving messages.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionCredentials.ModuleId))
                {
                    await UnsubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (_connectionCredentials.IsEdgeModule)
                    {
                        await UnsubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await UnsubscribeAsync(_moduleEventMessageTopic, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to disable receiving messages.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await SubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to enable receiving twin patches.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UnsubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to disable receiving twin patches.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex); ;
            }
        }

        public override async Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken)
        {
            if (!_isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken).ConfigureAwait(false);
                _isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(TwinGetTopicFormat.FormatInvariant(requestId))
                .WithQualityOfServiceLevel(_publishingQualityOfService)
                .Build();

            try
            {
                // Note the request as "in progress" before actually sending it so that no matter how quickly the service
                // responds, this layer can correlate the request.
                var taskCompletionSource = new TaskCompletionSource<GetTwinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                _getTwinResponseCompletions[requestId] = taskCompletionSource;
                _twinResponseTimeouts[requestId] = DateTimeOffset.UtcNow;

                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException(
                        $"Failed to publish the MQTT packet for getting this client's twin with reason code {result.ReasonCode}",
                        IotHubClientErrorCode.NetworkErrors);
                }

                if (Logging.IsEnabled)
                    Logging.Info($"Sent get twin request. Waiting on service response with request id {requestId}");

                // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                GetTwinResponse getTwinResponse = await GetTaskCompletionSourceResultAsync(
                        taskCompletionSource,
                        "Timed out waiting for the service to send the twin.",
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Received get twin response for request id {requestId} with status {getTwinResponse.Status}.");

                if (getTwinResponse.Status != 200)
                {
                    // Check if we have an int to string error code translation for the service returned error code.
                    // The error code could be a part of the service returned error message, or it can be a part of the topic string.
                    // We will check with the error code in the error message first (if present) since that is the more specific error code returned.
                    if ((Enum.TryParse(getTwinResponse.ErrorResponseMessage.ErrorCode.ToString(CultureInfo.InvariantCulture), out IotHubClientErrorCode errorCode)
                        || Enum.TryParse(getTwinResponse.Status.ToString(CultureInfo.InvariantCulture), out errorCode))
                        && Enum.IsDefined(typeof(IotHubClientErrorCode), errorCode))
                    {
                        throw new IotHubClientException(getTwinResponse.ErrorResponseMessage.Message, errorCode)
                        {
                            TrackingId = getTwinResponse.ErrorResponseMessage.TrackingId,
                        };
                    }

                    throw new IotHubClientException(getTwinResponse.ErrorResponseMessage.Message)
                    {
                        TrackingId = getTwinResponse.ErrorResponseMessage.TrackingId,
                    };
                }

                return getTwinResponse.Twin;
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This execption may be thrown even if cancellation has not been requested yet.
                // This case is treated as a timeout error rather than an OperationCanceledException
                throw new IotHubClientException(
                    MessageTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException(MessageTimedOutErrorMessage, ex);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to get the twin.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                // No matter what, remove the requestId from this dictionary since no thread will be waiting for it anymore
                _getTwinResponseCompletions.TryRemove(requestId, out _);
                _twinResponseTimeouts.TryRemove(requestId, out _);
            }
        }

        public override async Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken)
        {
            if (!_isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken).ConfigureAwait(false);
                _isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();
            string topic = string.Format(CultureInfo.InvariantCulture, TwinReportedPropertiesPatchTopicFormat, requestId);

            byte[] body = reportedProperties.GetObjectBytes();

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(_publishingQualityOfService)
                .WithPayload(body)
                .Build();

            try
            {
                // Note the request as "in progress" before actually sending it so that no matter how quickly the service
                // responds, this layer can correlate the request.
                var taskCompletionSource = new TaskCompletionSource<PatchTwinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                _reportedPropertyUpdateResponseCompletions[requestId] = taskCompletionSource;
                _twinResponseTimeouts[requestId] = DateTimeOffset.UtcNow;

                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException(
                        $"Failed to publish the MQTT packet for patching this client's twin with reason code {result.ReasonCode}",
                        IotHubClientErrorCode.NetworkErrors);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Sent twin patch request with request id {requestId}. Now waiting for the service response.");

                // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                PatchTwinResponse patchTwinResponse = await GetTaskCompletionSourceResultAsync(
                        taskCompletionSource,
                        "Timed out waiting for the service to send the updated reported properties version.",
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Received twin patch response for request id {requestId} with status {patchTwinResponse.Status}.");

                if (patchTwinResponse.Status != 204)
                {
                    // Check if we have an int to string error code translation for the service returned error code.
                    // The error code could be a part of the service returned error message, or it can be a part of the topic string.
                    // We will check with the error code in the error message first (if present) since that is the more specific error code returned.
                    if ((Enum.TryParse(patchTwinResponse.ErrorResponseMessage.ErrorCode.ToString(CultureInfo.InvariantCulture), out IotHubClientErrorCode errorCode)
                        || Enum.TryParse(patchTwinResponse.Status.ToString(CultureInfo.InvariantCulture), out errorCode))
                        && Enum.IsDefined(typeof(IotHubClientErrorCode), errorCode))
                    {
                        throw new IotHubClientException(patchTwinResponse.ErrorResponseMessage.Message, errorCode)
                        {
                            TrackingId = patchTwinResponse.ErrorResponseMessage.TrackingId,
                        };
                    }

                    throw new IotHubClientException(patchTwinResponse.ErrorResponseMessage.Message)
                    {
                        TrackingId = patchTwinResponse.ErrorResponseMessage.TrackingId,
                    };
                }

                return patchTwinResponse.Version;
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This execption may be thrown even if cancellation has not been requested yet.
                // This case is treated as a timeout error rather than an OperationCanceledException
                throw new IotHubClientException(
                    MessageTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException(MessageTimedOutErrorMessage, ex);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to send twin patch.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                // No matter what, remove the requestId from this dictionary since no thread will be waiting for it anymore
                _reportedPropertyUpdateResponseCompletions.TryRemove(requestId, out TaskCompletionSource<PatchTwinResponse> _);
                _twinResponseTimeouts.TryRemove(requestId, out DateTimeOffset _);
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            OnTransportClosedGracefully();

            try
            {
                _twinTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _mqttClient.DisconnectedAsync -= HandleDisconnectionAsync;
                _mqttClient.ApplicationMessageReceivedAsync -= HandleReceivedMessageAsync;
                await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Deliberately not rethrowing the exception because this is a "best effort" close.
                // The service may not have acknowledged that the client closed the connection, but
                // all local resources have been closed. The service will eventually realize the
                // connection is closed in cases like these.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Failed to gracefully close the MQTT client {ex}");
            }
        }

        protected private override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _mqttClient?.Dispose();
            _twinTimeoutTimer?.Dispose();
        }

        private bool CertificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            return _mqttTransportSettings.RemoteCertificateValidationCallback.Invoke(
                _mqttClient,
                args.Certificate,
                args.Chain,
                args.SslPolicyErrors);
        }

        private async Task SubscribeAsync(string topic, CancellationToken cancellationToken)
        {
            // "#" postfix is a multi-level wildcard in MQTT. When a client subscribes to a topic with a
            // multi-level wildcard, it receives all messages of a topic that begins with the pattern
            // before the wildcard character, no matter how long or deep the topic is.
            //
            // This is important to add here because topic strings will contain key-value pairs for metadata about
            // each message. For instance, a cloud to device message sent by the service with a correlation
            // id of "1" would have a topic like:
            // "devices/myDevice/messages/devicebound/?$cid=1"
            string fullTopic = topic + WildCardTopicFilter;

            MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(fullTopic, _receivingQualityOfService)
                .Build();

            try
            {
                MqttClientSubscribeResult subscribeResults = await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

                if (subscribeResults?.Items == null)
                {
                    throw new IotHubClientException(
                        $"Failed to subscribe to topic {fullTopic}",
                        IotHubClientErrorCode.NetworkErrors);
                }

                MqttClientSubscribeResultItem subscribeResult = subscribeResults.Items.FirstOrDefault();

                if (!subscribeResult.TopicFilter.Topic.Equals(fullTopic, StringComparison.Ordinal))
                {
                    throw new IotHubClientException(
                        $"Received unexpected subscription to topic {subscribeResult.TopicFilter.Topic}",
                        IotHubClientErrorCode.NetworkErrors);
                }
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This execption may be thrown even if cancellation has not been requested yet.
                // This case is treated as a timeout error rather than an OperationCanceledException
                throw new IotHubClientException(
                    SubscriptionTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException(SubscriptionTimedOutErrorMessage, ex);
            }
        }

        private async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken)
        {
            string fullTopic = topic + WildCardTopicFilter;

            MqttClientUnsubscribeOptions unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(fullTopic)
                    .Build();

            try
            {
                MqttClientUnsubscribeResult unsubscribeResults = await _mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken).ConfigureAwait(false);

                if (unsubscribeResults?.Items == null || unsubscribeResults.Items.Count != 1)
                {
                    throw new IotHubClientException(
                        $"Failed to unsubscribe to topic {fullTopic}",
                        IotHubClientErrorCode.NetworkErrors);
                }

                MqttClientUnsubscribeResultItem unsubscribeResult = unsubscribeResults.Items.FirstOrDefault();
                if (!unsubscribeResult.TopicFilter.Equals(fullTopic, StringComparison.Ordinal))
                {
                    throw new IotHubClientException(
                        $"Received unexpected unsubscription from topic {unsubscribeResult.TopicFilter}",
                        IotHubClientErrorCode.NetworkErrors);
                }

                if (unsubscribeResult.ResultCode != MqttClientUnsubscribeResultCode.Success)
                {
                    throw new IotHubClientException(
                        $"Failed to unsubscribe from topic {fullTopic} with reason {unsubscribeResult.ResultCode}",
                        IotHubClientErrorCode.NetworkErrors);
                }
            }
            catch (MqttCommunicationTimedOutException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This execption may be thrown even if cancellation has not been requested yet.
                // This case is treated as a timeout error rather than an OperationCanceledException
                throw new IotHubClientException(
                    UnsubscriptionTimedOutErrorMessage,
                    IotHubClientErrorCode.Timeout,
                    ex);
            }
            catch (MqttCommunicationTimedOutException ex) when (cancellationToken.IsCancellationRequested)
            {
                // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                // when the cancellation token requests cancellation.
                throw new OperationCanceledException(UnsubscriptionTimedOutErrorMessage, ex);
            }
        }

        private Task HandleDisconnectionAsync(MqttClientDisconnectedEventArgs disconnectedEventArgs)
        {
            if (Logging.IsEnabled)
                Logging.Info($"MQTT connection was lost {disconnectedEventArgs.Exception}");

            if (disconnectedEventArgs.ClientWasConnected)
            {
                OnTransportDisconnected();
            }

            return Task.CompletedTask;
        }

        private async Task HandleReceivedMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = false;
            string topic = receivedEventArgs.ApplicationMessage.Topic;

            if (topic.StartsWith(_deviceBoundMessagesTopic))
            {
                await HandleReceivedCloudToDeviceMessageAsync(receivedEventArgs).ConfigureAwait(false);
            }
            else if (topic.StartsWith(TwinDesiredPropertiesPatchTopic))
            {
                HandleReceivedDesiredPropertiesUpdateRequest(receivedEventArgs);
            }
            else if (topic.StartsWith(TwinResponseTopic))
            {
                HandleTwinResponse(receivedEventArgs);
            }
            else if (topic.StartsWith(DirectMethodsRequestTopic))
            {
                HandleReceivedDirectMethodRequest(receivedEventArgs);
            }
            else if (topic.StartsWith(_moduleEventMessageTopic)
                || topic.StartsWith(_edgeModuleInputEventsTopic))
            {
                // This works regardless of if the event is on a particular Edge module input or if
                // the module is not an Edge module.
                await HandleIncomingEventMessageAsync(receivedEventArgs).ConfigureAwait(false);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Received an MQTT message on unexpected topic {topic}. Ignoring message.");
            }
        }

        private async Task HandleReceivedCloudToDeviceMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            var receivedCloudToDeviceMessage = new IncomingMessage(payload)
            {
                PayloadConvention = _payloadConvention,
            };

            PopulateMessagePropertiesFromMqttMessage(receivedCloudToDeviceMessage, receivedEventArgs.ApplicationMessage);

            if (_messageReceivedListener != null)
            {
                MessageAcknowledgement acknowledgementType = await _messageReceivedListener.Invoke(receivedCloudToDeviceMessage);

                if (acknowledgementType != MessageAcknowledgement.Complete)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, "Cannot 'reject' or 'abandon' a received message over MQTT. Message will be acknowledged as 'complete' instead.");
                }

                // Note that MQTT does not support Abandon or Reject, so always Complete by acknowledging the message like this.
                try
                {
                    await receivedEventArgs.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // This likely happened because the connection was lost. The service will re-send this message so the user
                    // can acknowledge it on the new connection.
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"Failed to send the acknowledgement for a received cloud to device message {ex}"); ;
                }
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "Received a cloud to device message while user's callback for handling them was null. Disposing message.");
            }
        }

        private void HandleReceivedDirectMethodRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;

            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            string[] tokens = Regex.Split(receivedEventArgs.ApplicationMessage.Topic, "/", RegexOptions.Compiled);

            NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(tokens[4]);
            string requestId = queryStringKeyValuePairs.Get(RequestIdTopicKey);
            string methodName = tokens[3];

            var methodRequest = new DirectMethodRequest
            {
                PayloadConvention = _payloadConvention,
                MethodName = methodName,
                RequestId = requestId,
                Payload = payload,
            };

            // We are intentionally not awaiting _methodListener callback. The direct method response
            // is handled elsewhere, so we can simply invoke this callback and continue.
            _methodListener.Invoke(methodRequest).ConfigureAwait(false);
        }

        private void HandleReceivedDesiredPropertiesUpdateRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;

            string patch = _payloadConvention.PayloadEncoder.ContentEncoding.GetString(receivedEventArgs.ApplicationMessage.Payload);
            Dictionary<string, object> desiredPropertyPatchDictionary = _payloadConvention.PayloadSerializer.DeserializeToType<Dictionary<string, object>>(patch);
            var desiredPropertyPatch = new DesiredProperties(desiredPropertyPatchDictionary)
            {
                PayloadConvention = _payloadConvention,
            };

            _onDesiredStatePatchListener.Invoke(desiredPropertyPatch);
        }

        private void HandleTwinResponse(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;

            if (ParseResponseTopic(receivedEventArgs.ApplicationMessage.Topic, out string receivedRequestId, out int status, out long version))
            {
                byte[] payloadBytes = receivedEventArgs.ApplicationMessage.Payload ?? Array.Empty<byte>();

                if (_getTwinResponseCompletions.TryRemove(receivedRequestId, out TaskCompletionSource<GetTwinResponse> getTwinCompletion))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to get twin request with request id {receivedRequestId}.");

                    if (status != 200)
                    {
                        IotHubClientErrorResponseMessage errorResponse = null;

                        // This will only ever contain an error message which is encoded based on service contract (UTF-8).
                        if (payloadBytes.Length > 0)
                        {
                            string errorResponseString = Encoding.UTF8.GetString(payloadBytes);
                            try
                            {
                                errorResponse = JsonConvert.DeserializeObject<IotHubClientErrorResponseMessage>(errorResponseString);
                            }
                            catch (JsonException ex)
                            {
                                if (Logging.IsEnabled)
                                    Logging.Error(this, $"Failed to parse twin patch error response JSON. Message body: '{errorResponseString}'. Exception: {ex}. ");

                                errorResponse = new IotHubClientErrorResponseMessage
                                {
                                    Message = errorResponseString,
                                };
                            }
                        }

                        // This received message is in response to an update reported properties request.
                        var getTwinResponse = new GetTwinResponse
                        {
                            Status = status,
                            ErrorResponseMessage = errorResponse,
                        };

                        getTwinCompletion.TrySetResult(getTwinResponse);
                    }
                    else
                    {
                        try
                        {
                            // Use the encoder that has been agreed to between the client and service to decode the byte[] reasponse
                            // The response is deserialized into an SDK-defined type based on service-defined NewtonSoft.Json-based json property name.
                            // For this reason, we use NewtonSoft Json serializer for this deserialization.
                            TwinDocument clientTwinProperties = JsonConvert
                                .DeserializeObject<TwinDocument>(
                                    _payloadConvention
                                    .PayloadEncoder
                                    .ContentEncoding
                                    .GetString(payloadBytes));

                            var twinDesiredProperties = new DesiredProperties(clientTwinProperties.Desired)
                            {
                                PayloadConvention = _payloadConvention,
                            };

                            var twinReportedProperties = new ReportedProperties(clientTwinProperties.Reported, true)
                            {
                                PayloadConvention = _payloadConvention,
                            };

                            var getTwinResponse = new GetTwinResponse
                            {
                                Status = status,
                                Twin = new TwinProperties(twinDesiredProperties, twinReportedProperties),
                            };

                            getTwinCompletion.TrySetResult(getTwinResponse);
                        }
                        catch (JsonReaderException ex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Failed to parse Twin JSON.  Message body: '{Encoding.UTF8.GetString(payloadBytes)}'. Exception: {ex}.");

                            getTwinCompletion.TrySetException(ex);
                        }
                    }
                }
                else if (_reportedPropertyUpdateResponseCompletions.TryRemove(receivedRequestId, out TaskCompletionSource<PatchTwinResponse> patchTwinCompletion))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to patch twin request with request id {receivedRequestId}.");

                    IotHubClientErrorResponseMessage errorResponse = null;

                    // This will only ever contain an error message which is encoded based on service contract (UTF-8).
                    if (payloadBytes.Length > 0)
                    {
                        string errorResponseString = Encoding.UTF8.GetString(payloadBytes);
                        try
                        {
                            errorResponse = JsonConvert.DeserializeObject<IotHubClientErrorResponseMessage>(errorResponseString);
                        }
                        catch (JsonException ex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Failed to parse twin patch error response JSON. Message body: '{errorResponseString}'. Exception: {ex}. ");

                            errorResponse = new IotHubClientErrorResponseMessage
                            {
                                Message = errorResponseString,
                            };
                        }
                    }

                    // This received message is in response to an update reported properties request.
                    var patchTwinResponse = new PatchTwinResponse
                    {
                        Status = status,
                        Version = version,
                        ErrorResponseMessage = errorResponse,
                    };

                    patchTwinCompletion.TrySetResult(patchTwinResponse);
                }
                else
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to an unknown twin request with request id {receivedRequestId}. Discarding it.");
                }
            }
        }

        private async Task HandleIncomingEventMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;

            var iotHubMessage = new IncomingMessage(receivedEventArgs.ApplicationMessage.Payload)
            {
                PayloadConvention = _payloadConvention,
            };

            // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
            // We try to get the endpoint from the topic, if the topic is in the above format.
            string[] tokens = receivedEventArgs.ApplicationMessage.Topic.Split('/');

            // if there is an input name in the topic string, set the system property accordingly
            if (tokens.Length >= 6)
            {
                iotHubMessage.SystemProperties.Add(MessageSystemPropertyNames.InputName, tokens[5]);
            }

            await (_messageReceivedListener?.Invoke(iotHubMessage)).ConfigureAwait(false);
        }

        private bool ParseResponseTopic(string topicName, out string rid, out int status, out long version)
        {
            rid = "";
            status = 500;
            version = 0;

            // The topic here looks like
            // "$iothub/twin/res/204/?$rid=efc34c73-79ce-4054-9985-0cdf40a3c794&$version=2"
            // The regex matching splits it up into
            // "$iothub/twin" "res/204" "$rid=efc34c73-79ce-4054-9985-0cdf40a3c794&$version=2"
            // Then the third group is parsed for key value pairs such as "$version=2"
            Match match = _twinResponseTopicRegex.Match(topicName);
            if (match.Success)
            {
                // match.Groups[1] evaluates to the key-value pair that looks like "res/204"
                status = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);

                // match.Groups[1] evaluates to the query string key-value pair parameters
                NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(match.Groups[2].Value);
                rid = queryStringKeyValuePairs.Get(RequestIdTopicKey);

                if (status == 204)
                {
                    // This query string key-value pair is only expected in a successful patch twin response message.
                    // Get twin requests will contain the twin version in the payload instead.
                    version = int.Parse(queryStringKeyValuePairs.Get(VersionTopicKey), CultureInfo.InvariantCulture);
                }

                return true;
            }

            return false;
        }

        private void RemoveOldOperations(object _)
        {
            _ = _twinResponseTimeouts
                .Where(x => DateTimeOffset.UtcNow - x.Value > s_twinResponseTimeout)
                .Select(x =>
                    {
                        _getTwinResponseCompletions.TryRemove(x.Key, out TaskCompletionSource<GetTwinResponse> _);
                        _reportedPropertyUpdateResponseCompletions.TryRemove(x.Key, out TaskCompletionSource<PatchTwinResponse> _);
                        _twinResponseTimeouts.TryRemove(x.Key, out DateTimeOffset _);
                        return true;
                    });
        }

        private static void PopulateMessagePropertiesFromMqttMessage(IncomingMessage message, MqttApplicationMessage mqttMessage)
        {
            // Device bound messages could be in 2 formats, depending on whether it is going to the device, or to a module endpoint
            // Format 1 - going to the device - devices/{deviceId}/messages/devicebound/{properties}/
            // Format 2 - going to module endpoint - devices/{deviceId}/modules/{moduleId/endpoints/{endpointId}/{properties}/
            // So choose the right format to deserialize properties.
            string[] topicSegments = mqttMessage.Topic.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string propertiesSegment = topicSegments.Length > 6 ? topicSegments[6] : topicSegments[4];

            Dictionary<string, string> properties = UrlEncodedDictionarySerializer.Deserialize(propertiesSegment, 0);
            foreach (KeyValuePair<string, string> property in properties)
            {
                if (s_toSystemPropertiesMap.TryGetValue(property.Key, out string propertyName))
                {
                    message.SystemProperties[propertyName] = ConvertToSystemProperty(property);
                }
                else
                {
                    message.Properties[property.Key] = property.Value;
                }
            }
        }

        private static string ConvertFromSystemProperties(object systemProperty)
        {
            if (systemProperty is string stringProperty)
            {
                return stringProperty;
            }

            if (systemProperty is DateTime dateTimeProperty)
            {
                return dateTimeProperty.ToString("o", CultureInfo.InvariantCulture);
            }

            if (systemProperty is DateTimeOffset dateTimeOffsetProperty)
            {
                return dateTimeOffsetProperty.ToString("o", CultureInfo.InvariantCulture);
            }

            return systemProperty?.ToString();
        }

        private static object ConvertToSystemProperty(KeyValuePair<string, string> property)
        {
            if (string.IsNullOrEmpty(property.Value))
            {
                return property.Value;
            }

#pragma warning disable IDE0046 // More readable this way
            if (property.Key == IotHubWirePropertyNames.AbsoluteExpiryTime
                || property.Key == IotHubWirePropertyNames.CreationTimeUtc)
            {
                return DateTime.ParseExact(property.Value, "o", CultureInfo.InvariantCulture);
            }
#pragma warning restore IDE0046

            return property.Value;
        }

        internal static string PopulateMessagePropertiesFromMessage(string topicName, TelemetryMessage message)
        {
            var systemProperties = new Dictionary<string, string>(message.SystemProperties.Count);
            foreach (KeyValuePair<string, object> property in message.SystemProperties)
            {
                if (s_fromSystemPropertiesMap.TryGetValue(property.Key, out string propertyName))
                {
                    systemProperties[propertyName] = ConvertFromSystemProperties(property.Value);
                }
            }

            Dictionary<string, string> mergedProperties = systemProperties.Concat(message.Properties)
                .ToLookup(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value)
                .ToDictionary(keyValuePair => keyValuePair.Key, grouping => grouping.FirstOrDefault());

            string properties = UrlEncodedDictionarySerializer.Serialize(mergedProperties);

            string msg = $"{topicName}{properties}";

            // end the topic string with a '/' if it doesn't already end with one.
            if (!topicName.EndsWith("/", StringComparison.Ordinal))
            {
                msg += "/";
            }

            return msg;
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
