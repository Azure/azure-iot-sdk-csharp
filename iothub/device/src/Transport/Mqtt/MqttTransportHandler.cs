// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class MqttTransportHandler : TransportHandler, IDisposable
    {
        private const int ProtocolGatewayPort = 8883;

        private const string DeviceToCloudMessagesTopicFormat = "devices/{0}/messages/events/";
        private const string ModuleToCloudMessagesTopicFormat = "devices/{0}/modules/{1}/messages/events/";
        private readonly string _deviceToCloudMessagesTopic;
        private readonly string _moduleToCloudMessagesTopic;
        private readonly ConcurrentQueue<Message> _messageQueue;
        private readonly ConcurrentDictionary<string, PendingMqttTwinOperation> _pendingTwinOperations = new();

        // Topic names for receiving cloud-to-device messages.

        private const string DeviceBoundMessagesTopicFormat = "devices/{0}/messages/devicebound/";
        private readonly string _deviceBoundMessagesTopic;

        // Topic names for enabling input events on edge Modules.

        private const string EdgeModuleInputEventsTopicFormat = "devices/{0}/modules/{1}/inputs/";
        private readonly string _edgeModuleInputEventsTopic;

        // Topic names for enabling events on non-edge Modules.

        private const string ModuleEventMessageTopicFormat = "devices/{0}/modules/{1}/";
        private readonly string _moduleEventMessageTopic;

        private readonly bool _isEdgeModule;

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

        private readonly string _generationId = Guid.NewGuid().ToString();
        private static readonly int s_generationPrefixLength = Guid.NewGuid().ToString().Length;

        private const string BasicProxyAuthentication = "Basic";

        private const string ConnectTimedOutErrorMessage = "Timed out waiting for MQTT connection to open.";
        private const string MessageTimedOutErrorMessage = "Timed out waiting for MQTT message to be acknowledged.";
        private const string SubscriptionTimedOutErrorMessage = "Timed out waiting for MQTT subscription to be acknowledged.";
        private const string UnsubscriptionTimedOutErrorMessage = "Timed out waiting for MQTT unsubscription to be acknowledged.";

        private readonly MqttClientOptionsBuilder _mqttClientOptionsBuilder;

        private readonly MqttQualityOfServiceLevel _publishingQualityOfService;
        private readonly MqttQualityOfServiceLevel _receivingQualityOfService;

        private readonly Func<MethodRequestInternal, Task> _methodListener;
        private readonly Func<Message, Task> _deviceMessageListener;
        private readonly Func<string, Message, Task> _moduleMessageListener;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<Twin>> _twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<Twin>>();

        private ConcurrentDictionary<string, MqttApplicationMessageReceivedEventArgs> unacknowledgedCloudToDeviceMessages = new();

        private bool _isSubscribedToTwinResponses;
        private bool _isDeviceReceiveMessageCallbackSet;

        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly string _hostName;
        private readonly string _modelId;
        private readonly ProductInfo _productInfo;
        private readonly MqttTransportSettings _mqttTransportSettings;
        private readonly IotHubConnectionString _hubConnectionString;

        // Timer to check if any expired messages exist. The timer is executed after each hour of execution.
        private readonly Timer _twinTimeoutTimer;

        private static readonly Dictionary<string, string> s_toSystemPropertiesMap = new()
        {
            { IotHubWirePropertyNames.AbsoluteExpiryTime, MessageSystemPropertyNames.ExpiryTimeUtc },
            { IotHubWirePropertyNames.ConnectionDeviceId, MessageSystemPropertyNames.ConnectionDeviceId },
            { IotHubWirePropertyNames.ConnectionModuleId, MessageSystemPropertyNames.ConnectionModuleId },
            { IotHubWirePropertyNames.ContentEncoding, MessageSystemPropertyNames.ContentEncoding },
            { IotHubWirePropertyNames.ContentType, MessageSystemPropertyNames.ContentType },
            { IotHubWirePropertyNames.CorrelationId, MessageSystemPropertyNames.CorrelationId },
            { IotHubWirePropertyNames.CreationTimeUtc, MessageSystemPropertyNames.CreationTimeUtc },
            { IotHubWirePropertyNames.InterfaceId, MessageSystemPropertyNames.InterfaceId },
            { IotHubWirePropertyNames.MessageId, MessageSystemPropertyNames.MessageId },
            { IotHubWirePropertyNames.MessageSchema, MessageSystemPropertyNames.MessageSchema },
            { IotHubWirePropertyNames.MqttDiagIdKey, MessageSystemPropertyNames.DiagId },
            { IotHubWirePropertyNames.MqttDiagCorrelationContextKey, MessageSystemPropertyNames.DiagCorrelationContext },
            { IotHubWirePropertyNames.OutputName, MessageSystemPropertyNames.OutputName },
            { IotHubWirePropertyNames.To, MessageSystemPropertyNames.To },
            { IotHubWirePropertyNames.UserId, MessageSystemPropertyNames.UserId },
        };

        private static readonly Dictionary<string, string> s_fromSystemPropertiesMap = new()
        {
            { MessageSystemPropertyNames.ComponentName,IotHubWirePropertyNames.ComponentName },
            { MessageSystemPropertyNames.ContentType, IotHubWirePropertyNames.ContentType },
            { MessageSystemPropertyNames.ContentEncoding, IotHubWirePropertyNames.ContentEncoding },
            { MessageSystemPropertyNames.CorrelationId, IotHubWirePropertyNames.CorrelationId },
            { MessageSystemPropertyNames.CreationTimeUtc, IotHubWirePropertyNames.CreationTimeUtc },
            { MessageSystemPropertyNames.DiagId, IotHubWirePropertyNames.MqttDiagIdKey },
            { MessageSystemPropertyNames.DiagCorrelationContext, IotHubWirePropertyNames.MqttDiagCorrelationContextKey },
            { MessageSystemPropertyNames.ExpiryTimeUtc, IotHubWirePropertyNames.AbsoluteExpiryTime },
            { MessageSystemPropertyNames.InterfaceId, IotHubWirePropertyNames.InterfaceId },
            { MessageSystemPropertyNames.MessageId, IotHubWirePropertyNames.MessageId },
            { MessageSystemPropertyNames.MessageSchema, IotHubWirePropertyNames.MessageSchema },
            { MessageSystemPropertyNames.OutputName, IotHubWirePropertyNames.OutputName },
            { MessageSystemPropertyNames.To, IotHubWirePropertyNames.To },
            { MessageSystemPropertyNames.UserId, IotHubWirePropertyNames.UserId },
        };

        internal IMqttClient _mqttClient;

        private MqttClientOptions _mqttClientOptions;
        internal static readonly char[] separator = new[] { '/' };

        public MqttTransportHandler(
                    PipelineContext context,
                    IotHubConnectionString iotHubConnectionString,
                    MqttTransportSettings settings,
                    Func<MethodRequestInternal, Task> onMethodCallback = null,
                    Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
                    Func<string, Message, Task> onModuleMessageReceivedCallback = null,
                    Func<Message, Task> onDeviceMessageReceivedCallback = null)
            : base(context, settings)
        {
            _methodListener = onMethodCallback;
            _deviceMessageListener = onDeviceMessageReceivedCallback;
            _moduleMessageListener = onModuleMessageReceivedCallback;
            _onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;

            _mqttTransportSettings = settings;
            _deviceId = iotHubConnectionString.DeviceId;
            _moduleId = iotHubConnectionString.ModuleId;

            _deviceToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceToCloudMessagesTopicFormat, _deviceId);
            _moduleToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, ModuleToCloudMessagesTopicFormat, _deviceId, _moduleId);
            _deviceBoundMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicFormat, _deviceId);
            _moduleEventMessageTopic = string.Format(CultureInfo.InvariantCulture, ModuleEventMessageTopicFormat, _deviceId, _moduleId);
            _edgeModuleInputEventsTopic = string.Format(CultureInfo.InvariantCulture, EdgeModuleInputEventsTopicFormat, _deviceId, _moduleId);

            _modelId = context.ClientOptions.ModelId;
            _productInfo = context.ProductInfo;

            var mqttFactory = new MqttClientFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttClientOptionsBuilder = new MqttClientOptionsBuilder();
            _messageQueue = new ConcurrentQueue<Message>();

            _hubConnectionString = iotHubConnectionString;
            _hostName = iotHubConnectionString.HostName;

            _isEdgeModule = !string.IsNullOrWhiteSpace(iotHubConnectionString.ModuleId) && iotHubConnectionString.IsUsingGateway;

            _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessageAsync;
            _mqttClient.DisconnectedAsync += HandleDisconnectionAsync;

            if (_mqttTransportSettings.GetTransportType() != TransportType.Mqtt_WebSocket_Only)
            {
                _mqttClientOptionsBuilder.WithTcpServer(_hostName, ProtocolGatewayPort);
            }
            else
            {
                string uriString = $"wss://{_hostName}/$iothub/websocket";
                _mqttClientOptionsBuilder.WithWebSocketServer(options =>
                {
                    options.WithUri(uriString);

                    IWebProxy proxy = _mqttTransportSettings.Proxy;
                    if (proxy != null)
                    {
                        Uri serviceUri = new(uriString);
                        Uri proxyUri = _mqttTransportSettings.Proxy.GetProxy(serviceUri);

                        options.WithProxyOptions(proxyOptions =>
                        {

                            if (proxy.Credentials != null)
                            {
                                NetworkCredential credentials = proxy.Credentials.GetCredential(serviceUri, BasicProxyAuthentication);
                                string username = credentials.UserName;
                                string password = credentials.Password;
                                proxyOptions.WithUsername(username);
                                proxyOptions.WithPassword(password);
                            }

                            proxyOptions.WithAddress(proxyUri.AbsoluteUri);
                        });
                    }
                });
            }

            _mqttClientOptionsBuilder.WithTlsOptions(tlsOptions =>
            {
                List<X509Certificate2> certs = settings.ClientCertificate == null
                    ? new List<X509Certificate2>(0)
                    : new List<X509Certificate2> { settings.ClientCertificate };

                tlsOptions.WithClientCertificates(certs);
                tlsOptions.WithIgnoreCertificateRevocationErrors(!_mqttTransportSettings.CertificateRevocationCheck);

                if (_mqttTransportSettings?.RemoteCertificateValidationCallback != null)
                {
                    tlsOptions.WithCertificateValidationHandler(CertificateValidationHandler);
                }

                tlsOptions.UseTls(true);
                tlsOptions.WithSslProtocols(TlsVersions.Instance.Preferred);
            });

            _mqttClientOptionsBuilder
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311) // 3.1.1
                .WithCleanSession(_mqttTransportSettings.CleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttTransportSettings.KeepAliveInSeconds))
                .WithTimeout(TimeSpan.FromMilliseconds(-1)); // MQTTNet will only time out if the cancellation token requests cancellation.

            if (_mqttTransportSettings.WillMessage != null)
            {
                _mqttClientOptionsBuilder
                    .WithWillTopic(_deviceToCloudMessagesTopic)
                    .WithWillPayload(_mqttTransportSettings.WillMessage.Payload);

                if (_mqttTransportSettings.WillMessage.QualityOfService == MqttQualityOfServiceLevel.AtMostOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
                }
                else if (_mqttTransportSettings.WillMessage.QualityOfService == MqttQualityOfServiceLevel.AtLeastOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
                }
            }

            _publishingQualityOfService = _mqttTransportSettings.PublishToServerQoS == MqttQualityOfServiceLevel.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            _receivingQualityOfService = _mqttTransportSettings.ReceivingQoS == MqttQualityOfServiceLevel.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            // Create a timer to remove any expired messages.
            _twinTimeoutTimer = new Timer(RemoveOldOperations);
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

            try
            {
                string clientId = string.IsNullOrWhiteSpace(_moduleId)
                    ? _deviceId
                    : $"{_deviceId}/{_moduleId}";
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

                if (_hubConnectionString.SharedAccessSignature != null)
                {
                    // Symmetric key authenticated connections need to set client Id, username, and password
                    string password = _hubConnectionString.SharedAccessSignature;
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
                    MqttClientConnectResult connack = await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken).ConfigureAwait(false);
                    if (connack.ResultCode != MqttClientConnectResultCode.Success)
                    {
                        switch (connack.ResultCode)
                        {
                            case MqttClientConnectResultCode.BadUserNameOrPassword:
                            case MqttClientConnectResultCode.NotAuthorized:
                            case MqttClientConnectResultCode.ClientIdentifierNotValid:
                                throw new IotHubClientException(
                                    "Failed to open the MQTT connection due to incorrect or unauthorized credentials",
                                    IotHubClientErrorCode.Unauthorized);
                            case MqttClientConnectResultCode.UnsupportedProtocolVersion:
                                // Should never happen since the protocol version (3.1.1) is hardcoded
                                throw new IotHubClientException(
                                    "Failed to open the MQTT connection due to an unsupported MQTT version");
                            case MqttClientConnectResultCode.ServerUnavailable:
                                throw new IotHubClientException(
                                    "MQTT connection rejected because the server was unavailable",
                                    IotHubClientErrorCode.ServerBusy);
                            default:
                                // MQTT 3.1.1 only supports the above connect return codes, so this default case
                                // should never happen. For more details, see the MQTT 3.1.1 specification section "3.2.2.3 Connect Return code"
                                // https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html
                                // MQTT 5 supports a larger set of connect codes. See the MQTT 5.0 specification section "3.2.2.2 Connect Reason Code"
                                // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901074
                                throw new IotHubClientException("Failed to open the MQTT connection. MQTT connack code: " + connack.ResultCode + " with reason: " + connack.ReasonString, IotHubClientErrorCode.NetworkErrors);
                        }
                    }
                }
                catch (MqttCommunicationTimedOutException ex)
                {
                    throw new IotHubClientException(
                        ConnectTimedOutErrorMessage,
                        IotHubClientErrorCode.Timeout,
                        ex);
                }
                catch (MqttCommunicationException ex)
                {
                    if (ex.InnerException is MqttCommunicationTimedOutException)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                            // when the cancellation token requests cancellation.
                            throw new OperationCanceledException(ConnectTimedOutErrorMessage);
                        }

                        // This execption may be thrown even if cancellation has not been requested yet.
                        // This case is treated as a timeout error rather than an OperationCanceledException
                        throw new IotHubClientException(ConnectTimedOutErrorMessage, IotHubClientErrorCode.Timeout);
                    }

                    throw new IotHubClientException("An unknown error occurred when connecting. See inner error for details", IotHubClientErrorCode.NetworkErrors, ex);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(OpenAsync));
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Topic depends on if the client is a module client or a device client
                string baseTopicName = _moduleId == null ? _deviceToCloudMessagesTopic : _moduleToCloudMessagesTopic;
                string topicName = PopulateMessagePropertiesFromMessage(baseTopicName, message);

                MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topicName)
                    .WithPayload(message.GetBytes())
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
                catch (MqttCommunicationTimedOutException ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // MQTTNet throws MqttCommunicationTimedOutException instead of OperationCanceledException
                        // when the cancellation token requests cancellation.
                        throw new OperationCanceledException(MessageTimedOutErrorMessage, ex);
                    }

                    // This execption may be thrown even if cancellation has not been requested yet.
                    // This case is treated as a timeout error rather than an OperationCanceledException
                    throw new IotHubClientException(
                        MessageTimedOutErrorMessage,
                        IotHubClientErrorCode.Timeout,
                        ex);
                }
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not InvalidOperationException)
            {
                if (ex is OperationCanceledException
                    && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                if (message.Properties.ContainsKey("AzIoTHub_FaultOperationType"))
                {
                    // When fault injection causes this operation to fail, the MQTT layer throws a MqttClientDisconnectedException.
                    // Normally, we don't want that to get to the device app, but for fault injection tests we prefer
                    // an exception that is NOT retryable so we'll let this exception slip through.
                    throw;
                }

                throw new IotHubClientException(
                    $"Failed to send message with message Id: [{message.MessageId}].",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            Debug.Fail("This should be caught by the client, but added here just in case.");
            throw new InvalidOperationException("This operation is not supported over MQTT. Please refer to the API comments for additional details.");
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableMethodsAsync));
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodResponse, cancellationToken, nameof(SendMethodResponseAsync));

            string topic = DirectMethodsResponseTopicFormat.FormatInvariant(methodResponse.Status, methodResponse.RequestId);
            byte[] serializedPayload = methodResponse.GetBytes();
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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, methodResponse, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            if (isAnEdgeModule)
            {
                await SubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken);
            }
            else
            {
                await SubscribeAsync(_moduleEventMessageTopic, cancellationToken);
            }
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            if (isAnEdgeModule)
            {
                await UnsubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken);
            }
            else
            {
                await UnsubscribeAsync(_moduleEventMessageTopic, cancellationToken);
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

            try
            {
                await SubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = true;
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to enable receiving messages.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableReceiveMessageAsync));
            }
        }

        public override async Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the device connects with a CleanSession flag set to false, we will need to deliver the messages
            // that were sent before the client had subscribed to the C2D message receive topic.
            if (!_mqttTransportSettings.CleanSession)
            {
                Message message = null;
                // Received C2D messages are enqueued into _messageQueue.
                while (!_messageQueue.IsEmpty)
                {
                    if(_messageQueue.TryDequeue(out message))
                    {
                        await HandleReceivedCloudToDeviceMessageAsync(message).ConfigureAwait(false);
                        message.Dispose();
                    }
                }
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

            try
            {
                await UnsubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
                _isDeviceReceiveMessageCallbackSet = false;
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to disable receiving messages.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableReceiveMessageAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableTwinPatchAsync));
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

            try
            {
                await UnsubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && ex is not OperationCanceledException)
            {
                throw new IotHubClientException(
                    "Failed to disable receiving twin patches.",
                    IotHubClientErrorCode.NetworkErrors,
                    ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableTwinPatchAsync));
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(SendTwinGetAsync));

            try
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
                    _pendingTwinOperations[requestId] = new PendingMqttTwinOperation(taskCompletionSource);

                    MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        throw new IotHubClientException(
                            $"Failed to publish the MQTT packet for getting this client's twin with reason code {result.ReasonCode}",
                            IotHubClientErrorCode.NetworkErrors);
                    }

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Sent get twin request. Waiting on service response with request id {requestId}", nameof(SendTwinGetAsync));

                    // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                    GetTwinResponse getTwinResponse = await taskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received get twin response for request id {requestId} with status {getTwinResponse.Status}.", nameof(SendTwinGetAsync));

                    if (getTwinResponse.Status != 200)
                    {
                        // Check if we have an int to string error code translation for the service returned error code.
                        // The error code could be a part of the service returned error message, or it can be a part of the topic string.
                        // We will check with the error code in the error message first (if present) since that is the more specific error code returned.
                        if ((Enum.TryParse(getTwinResponse.ErrorResponseMessage.ErrorCode.ToString(CultureInfo.InvariantCulture), out IotHubClientErrorCode errorCode)
                            || Enum.TryParse(getTwinResponse.Status.ToString(CultureInfo.InvariantCulture), out errorCode))
                            && Enum.IsDefined(errorCode))
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
                    _pendingTwinOperations.TryRemove(requestId, out _);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(SendTwinGetAsync));
            }
        }

        public override async Task<long> SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));

            try
            {
                if (!_isSubscribedToTwinResponses)
                {
                    await SubscribeAsync(TwinResponseTopic, cancellationToken).ConfigureAwait(false);
                    _isSubscribedToTwinResponses = true;
                }

                string requestId = Guid.NewGuid().ToString();
                string topic = string.Format(CultureInfo.InvariantCulture, TwinReportedPropertiesPatchTopicFormat, requestId);

                byte[] body = Encoding.UTF8.GetBytes(reportedProperties.ToJson());

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
                    _pendingTwinOperations[requestId] = new PendingMqttTwinOperation(taskCompletionSource);

                    MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        throw new IotHubClientException(
                            $"Failed to publish the MQTT packet for patching this client's twin with reason code {result.ReasonCode}",
                            IotHubClientErrorCode.NetworkErrors);
                    }

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Sent twin patch request with request id {requestId}. Now waiting for the service response.", nameof(SendTwinPatchAsync));

                    // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                    PatchTwinResponse patchTwinResponse = await taskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received twin patch response for request id {requestId} with status {patchTwinResponse.Status}.", nameof(SendTwinPatchAsync));

                    if (patchTwinResponse.Status != 204)
                    {
                        // Check if we have an int to string error code translation for the service returned error code.
                        // The error code could be a part of the service returned error message, or it can be a part of the topic string.
                        // We will check with the error code in the error message first (if present) since that is the more specific error code returned.
                        if ((Enum.TryParse(patchTwinResponse.ErrorResponseMessage.ErrorCode.ToString(CultureInfo.InvariantCulture), out IotHubClientErrorCode errorCode)
                            || Enum.TryParse(patchTwinResponse.Status.ToString(CultureInfo.InvariantCulture), out errorCode))
                            && Enum.IsDefined<IotHubClientErrorCode>(errorCode))
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
                    _pendingTwinOperations.TryRemove(requestId, out _);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));
            }
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Completing a message with lockToken: {lockToken}", nameof(CompleteAsync));

            cancellationToken.ThrowIfCancellationRequested();

            if (_receivingQualityOfService == MqttQualityOfServiceLevel.AtMostOnce)
            {
                throw new IotHubException("Complete is not allowed for QoS 0.", isTransient: false);
            }

            if (!unacknowledgedCloudToDeviceMessages.TryRemove(lockToken, out var args))
            {
                throw new IotHubException(
                    "Lock token is stale or never existed. The message will be redelivered. Please discard this lock token and do not retry the operation.",
                    isTransient: false);
            }

            await args.AcknowledgeAsync(cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Completing a message with lockToken: {lockToken}", nameof(CompleteAsync));
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotSupportedException("MQTT protocol does not support this operation");
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotSupportedException("MQTT protocol does not support this operation");
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

            OnTransportClosedGracefully();

            try
            {
                _twinTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _mqttClient.ApplicationMessageReceivedAsync -= HandleReceivedMessageAsync;
            _mqttClient.DisconnectedAsync -= HandleDisconnectionAsync;
            _mqttClient?.Dispose();
            _twinTimeoutTimer.Dispose();
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
                Logging.Info(this, $"MQTT connection was lost {disconnectedEventArgs.Exception}", nameof(HandleDisconnectionAsync));

            OnTransportDisconnected();

            // During a disconnection, any pending twin updates won't be received, so we'll preemptively
            // cancel these operations so the client can retry once reconnected.
            RemoveOldOperations(TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private static Message ProcessC2DMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload.ToArray();

            var receivedCloudToDeviceMessage = new Message(payload);

            PopulateMessagePropertiesFromMqttMessage(receivedCloudToDeviceMessage, receivedEventArgs.ApplicationMessage);

            return receivedCloudToDeviceMessage;
        }

        private async Task HandleReceivedMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = false;
            string topic = receivedEventArgs.ApplicationMessage.Topic;

            if (topic.StartsWith(_deviceBoundMessagesTopic, StringComparison.InvariantCulture))
            {
                // If C2D message callback is not set, messages are added to a queue to be processed later.
                // However, the messages are still being ack'ed right away.
                using Message c2dMessage = ProcessC2DMessage(receivedEventArgs);
                c2dMessage.LockToken = Guid.NewGuid().ToString();
                unacknowledgedCloudToDeviceMessages.TryAdd(c2dMessage.LockToken, receivedEventArgs);
                await HandleReceivedCloudToDeviceMessageAsync(c2dMessage).ConfigureAwait(false);
                receivedEventArgs.AutoAcknowledge = false; // delay the ack until user calls "CompleteAsync"
            }
            else if (topic.StartsWith(TwinDesiredPropertiesPatchTopic, StringComparison.InvariantCulture))
            {
                HandleReceivedDesiredPropertiesUpdateRequest(receivedEventArgs);
            }
            else if (topic.StartsWith(TwinResponseTopic, StringComparison.InvariantCulture))
            {
                HandleTwinResponse(receivedEventArgs);
            }
            else if (topic.StartsWith(DirectMethodsRequestTopic, StringComparison.InvariantCulture))
            {
                HandleReceivedDirectMethodRequest(receivedEventArgs);
            }
            else if (topic.StartsWith(_moduleEventMessageTopic, StringComparison.InvariantCulture)
                || topic.StartsWith(_edgeModuleInputEventsTopic, StringComparison.InvariantCulture))
            {
                // This works regardless of if the event is on a particular Edge module input or if
                // the module is not an Edge module.
                await HandleIncomingEventMessageAsync(receivedEventArgs).ConfigureAwait(false);
            }
            else if (Logging.IsEnabled)
            {
                Logging.Error(this, $"Received an MQTT message on unexpected topic {topic}. Ignoring message.");
            }
        }

        private async Task HandleReceivedCloudToDeviceMessageAsync(Message receivedCloudToDeviceMessage)
        {
            if (_deviceMessageListener != null && _isDeviceReceiveMessageCallbackSet)
            {
                await _deviceMessageListener.Invoke(receivedCloudToDeviceMessage);
            }
            else if(!_isDeviceReceiveMessageCallbackSet)
            {
                _messageQueue.Enqueue(receivedCloudToDeviceMessage);
                while(_messageQueue.Count > _mqttTransportSettings.IncomingMessageQueueSize)
                {
                    _messageQueue.TryDequeue(out _);
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"Queue size of {_mqttTransportSettings.IncomingMessageQueueSize} for C2D messages has been reached, removing oldest queued C2D message. " +
                            $"To avoid losing further messages, set SetIncomingMessageCallbackAsync() to process the messages or increase message queue size in IotHubClientMqttSettings.");
                    }
                }
            }
            else if (Logging.IsEnabled)
            {
                Logging.Error(this, "Received a cloud to device message while user's callback for handling them was null. Disposing message.");
            }
        }

        private void HandleReceivedDirectMethodRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;

            byte[] payload = receivedEventArgs.ApplicationMessage.Payload.ToArray();

            string[] tokens = Regex.Split(receivedEventArgs.ApplicationMessage.Topic, "/", RegexOptions.Compiled);

            NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(tokens[4]);
            string requestId = queryStringKeyValuePairs.Get(RequestIdTopicKey);
            string methodName = tokens[3];

            using var methodRequest = new MethodRequestInternal(methodName, requestId, new MemoryStream(payload), CancellationToken.None);

            // We are intentionally not awaiting _methodListener callback. The direct method response
            // is handled elsewhere, so we can simply invoke this callback and continue.
            _methodListener.Invoke(methodRequest).ConfigureAwait(false);
        }

        private void HandleReceivedDesiredPropertiesUpdateRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;
            var twinProperties = JsonConvert.DeserializeObject<TwinCollection>(Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload.ToArray()), JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            _onDesiredStatePatchListener.Invoke(twinProperties);
        }

        private void HandleTwinResponse(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            // This message is always QoS 0, so no ack will be sent.
            receivedEventArgs.AutoAcknowledge = true;

            if (ParseResponseTopic(receivedEventArgs.ApplicationMessage.Topic, out string receivedRequestId, out int status, out long version))
            {
                byte[] payloadBytes = receivedEventArgs.ApplicationMessage.Payload.ToArray() ?? Array.Empty<byte>();

                if (_pendingTwinOperations.TryRemove(receivedRequestId, out PendingMqttTwinOperation twinOperation))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to patch twin request with request id {receivedRequestId}.", nameof(HandleTwinResponse));

                    IotHubClientErrorResponseMessage ParseError(byte[] payloadBytes)
                    {
                        // This will only ever contain an error message which is encoded based on service contract (UTF-8).
                        if (payloadBytes.Length == 0)
                        {
                            return null;
                        }

                        string errorResponseString = Encoding.UTF8.GetString(payloadBytes);
                        try
                        {
                            return System.Text.Json.JsonSerializer.Deserialize<IotHubClientErrorResponseMessage>(errorResponseString);
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Failed to parse twin patch error response JSON. Message body: '{errorResponseString}'. Exception: {ex}. ", nameof(HandleTwinResponse));

                            return new IotHubClientErrorResponseMessage
                            {
                                Message = errorResponseString,
                            };
                        }
                    }

                    if (twinOperation.TwinPatchTask != null)
                    {
                        IotHubClientErrorResponseMessage error = status == 204
                            ? null
                            : ParseError(payloadBytes);

                        // This received message is in response to an update reported properties request.
                        var patchTwinResponse = new PatchTwinResponse
                        {
                            Status = status,
                            Version = version,
                            ErrorResponseMessage = error,
                        };

                        twinOperation.TwinPatchTask.TrySetResult(patchTwinResponse);
                    }
                    else // should be a "get twin" operation
                    {
                        var getTwinResponse = new GetTwinResponse
                        {
                            Status = status,
                        };

                        if (status != 200)
                        {
                            getTwinResponse.ErrorResponseMessage = ParseError(payloadBytes);
                            twinOperation.TwinResponseTask.TrySetResult(getTwinResponse);
                        }
                        else
                        {
                            TwinProperties getTwin = System.Text.Json.JsonSerializer.Deserialize<TwinProperties>(payloadBytes);

                            getTwinResponse.Twin = new Twin(getTwin);

                            twinOperation.TwinResponseTask.TrySetResult(getTwinResponse);
                        }
                    }
                }
                else if (Logging.IsEnabled)
                {
                    Logging.Info(this, $"Received response to an unknown twin request with request id {receivedRequestId}. Discarding it.", nameof(HandleTwinResponse));
                }
            }
        }

        private void RemoveOldOperations(object state)
        {
            if (state is not TimeSpan maxAge)
            {
                maxAge = TimeSpan.FromMinutes(5);
            }

            if (Logging.IsEnabled)
                Logging.Info(this, $"Removing operations older than {maxAge}", nameof(RemoveOldOperations));

            const string exceptionMessage = "Did not receive twin response from service.";
            int canceledOperations = _pendingTwinOperations
                .Where(x => DateTimeOffset.UtcNow - x.Value.RequestSentOnUtc > maxAge)
                .Select(x =>
                {
                    if (_pendingTwinOperations.TryRemove(x.Key, out PendingMqttTwinOperation pendingOperation))
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"Removing twin response for {x.Key}", nameof(RemoveOldOperations));

                        pendingOperation.TwinResponseTask?.TrySetException(new IotHubClientException(exceptionMessage, IotHubClientErrorCode.NetworkErrors));
                        pendingOperation.TwinPatchTask?.TrySetException(new IotHubClientException(exceptionMessage, IotHubClientErrorCode.NetworkErrors));
                    }
                    return true;
                })
                .Count();

            if (Logging.IsEnabled)
                Logging.Error(this, $"Removed {canceledOperations} twin responses", nameof(RemoveOldOperations));
        }

        private async Task HandleIncomingEventMessageAsync(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;

            using var iotHubMessage = new Message(receivedEventArgs.ApplicationMessage.Payload.ToArray());

            // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
            // We try to get the endpoint from the topic, if the topic is in the above format.
            string[] tokens = receivedEventArgs.ApplicationMessage.Topic.Split('/');

            // if there is an input name in the topic string, set the system property accordingly
            if (tokens.Length >= 6)
            {
                iotHubMessage.SystemProperties.Add(MessageSystemPropertyNames.InputName, tokens[5]);
                await (_moduleMessageListener?.Invoke(tokens[5], iotHubMessage)).ConfigureAwait(false);
            }
            else
            { 
                await (_moduleMessageListener?.Invoke(null, iotHubMessage)).ConfigureAwait(false);
            }

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
            if (!match.Success)
            {
                return false;
            }

            // match.Groups[1] evaluates to the key-value pair that looks like "res/204"
            status = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);

            // match.Groups[1] evaluates to the query string key-value pair parameters
            NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(match.Groups[2].Value);
            rid = queryStringKeyValuePairs.Get(RequestIdTopicKey);

            if (status == 204)
            {
                // This query string key-value pair is only expected in a successful patch twin response message.
                // Get twin requests will contain the twin version in the payload instead.
                _ = long.TryParse(queryStringKeyValuePairs.Get(VersionTopicKey), out version);
            }

            return true;
        }

        private static void PopulateMessagePropertiesFromMqttMessage(Message message, MqttApplicationMessage mqttMessage)
        {
            // Device bound messages could be in 2 formats, depending on whether it is going to the device, or to a module endpoint
            // Format 1 - going to the device - devices/{deviceId}/messages/devicebound/{properties}/
            // Format 2 - going to module endpoint - devices/{deviceId}/modules/{moduleId/endpoints/{endpointId}/{properties}/
            // So choose the right format to deserialize properties.
            string[] topicSegments = mqttMessage.Topic.Split(separator, StringSplitOptions.RemoveEmptyEntries);
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

        internal static string PopulateMessagePropertiesFromMessage(string topicName, Message message)
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

            // end the topic string with a '/' if it doesn't already end with one.
            string suffix = topicName.EndsWith('/')
                ? string.Empty
                : "/";
            return $"{topicName}{properties}{suffix}";
        }
    }
}
