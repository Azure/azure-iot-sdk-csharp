// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.Azure.Devices.Client.Exceptions;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class MqttTransportHandler : TransportHandler
    {
        private const int ProtocolGatewayPort = 8883;
        private const int MaxMessageSize = 256 * 1024;
        private const int MaxTopicNameLength = 65535;

        private const string DeviceToCloudMessagesTopicFormat = "devices/{0}/messages/events/";
        private const string ModuleToCloudMessagesTopicFormat = "devices/{0}/modules/{1}/messages/events/";
        private string _deviceToCloudMessagesTopic;
        private string _moduleToCloudMessagesTopic;

        // Topic names for receiving cloud-to-device messages.

        private const string DeviceBoundMessagesTopicFormat = "devices/{0}/messages/devicebound/";
        private string _deviceBoundMessagesTopic;

        // Topic names for enabling input events on edge Modules.

        private const string EdgeModuleInputEventsTopicFormat = "devices/{0}/modules/{1}/inputs/";
        private string _edgeModuleInputEventsTopic;

        // Topic names for enabling events on non-edge Modules.

        private const string ModuleEventMessageTopicFormat = "devices/{0}/modules/{1}/";
        private string _moduleEventMessageTopic;

        // Topic names for retrieving a device's twin properties.
        // The client first subscribes to "$iothub/twin/res/#", to receive the operation's responses.
        // It then sends an empty message to the topic "$iothub/twin/GET/?$rid={request id}, with a populated value for request Id.
        // The service then sends a response message containing the device twin data on topic "$iothub/twin/res/{status}/?$rid={request id}", using the same request Id as the request.

        private const string TwinResponseTopic = "$iothub/twin/res/";
        private const string TwinGetTopicFormat = "$iothub/twin/GET/?$rid={0}";
        private const string TwinResponseTopicPattern = @"\$iothub/twin/res/(\d+)/(\?.+)";
        private readonly Regex _twinResponseTopicRegex = new Regex(TwinResponseTopicPattern, RegexOptions.Compiled);

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

        // Intentionally not private so that unit tests can mock this field
        internal IMqttClient _mqttClient;

        private MqttClientOptions _mqttClientOptions;
        private MqttClientOptionsBuilder _mqttClientOptionsBuilder;

        private MqttQualityOfServiceLevel publishingQualityOfService;
        private MqttQualityOfServiceLevel receivingQualityOfService;

        private readonly Func<DirectMethodRequest, Task> _methodListener;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly Func<Message, Task> _moduleMessageReceivedListener;
        private readonly Func<Message, Task> _deviceMessageReceivedListener;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<GetTwinResponse>> _getTwinResponseCompletions = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<PatchTwinResponse>> _reportedPropertyUpdateResponseCompletions = new();

        private bool _isSubscribedToTwinResponses;

        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly string _hostName;
        private readonly string _modelId;
        private readonly ProductInfo _productInfo;
        private readonly IotHubClientMqttSettings _mqttTransportSettings;
        private readonly IotHubConnectionCredentials _connectionCredentials;

        private static readonly Dictionary<string, string> s_toSystemPropertiesMap = new Dictionary<string, string>
        {
            {IotHubWirePropertyNames.AbsoluteExpiryTime, MessageSystemPropertyNames.ExpiryTimeUtc},
            {IotHubWirePropertyNames.CorrelationId, MessageSystemPropertyNames.CorrelationId},
            {IotHubWirePropertyNames.MessageId, MessageSystemPropertyNames.MessageId},
            {IotHubWirePropertyNames.To, MessageSystemPropertyNames.To},
            {IotHubWirePropertyNames.UserId, MessageSystemPropertyNames.UserId},
            {IotHubWirePropertyNames.MessageSchema, MessageSystemPropertyNames.MessageSchema},
            {IotHubWirePropertyNames.CreationTimeUtc, MessageSystemPropertyNames.CreationTimeUtc},
            {IotHubWirePropertyNames.ContentType, MessageSystemPropertyNames.ContentType},
            {IotHubWirePropertyNames.ContentEncoding, MessageSystemPropertyNames.ContentEncoding},
            {MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation},
            {MessageSystemPropertyNames.Ack, MessageSystemPropertyNames.Ack},
            {IotHubWirePropertyNames.ConnectionDeviceId, MessageSystemPropertyNames.ConnectionDeviceId },
            {IotHubWirePropertyNames.ConnectionModuleId, MessageSystemPropertyNames.ConnectionModuleId },
            {IotHubWirePropertyNames.MqttDiagIdKey, MessageSystemPropertyNames.DiagId},
            {IotHubWirePropertyNames.MqttDiagCorrelationContextKey, MessageSystemPropertyNames.DiagCorrelationContext},
            {IotHubWirePropertyNames.InterfaceId, MessageSystemPropertyNames.InterfaceId}
        };

        private static readonly Dictionary<string, string> s_fromSystemPropertiesMap = new Dictionary<string, string>
        {
            {MessageSystemPropertyNames.ExpiryTimeUtc, IotHubWirePropertyNames.AbsoluteExpiryTime},
            {MessageSystemPropertyNames.CorrelationId, IotHubWirePropertyNames.CorrelationId},
            {MessageSystemPropertyNames.MessageId, IotHubWirePropertyNames.MessageId},
            {MessageSystemPropertyNames.To, IotHubWirePropertyNames.To},
            {MessageSystemPropertyNames.UserId, IotHubWirePropertyNames.UserId},
            {MessageSystemPropertyNames.MessageSchema, IotHubWirePropertyNames.MessageSchema},
            {MessageSystemPropertyNames.CreationTimeUtc, IotHubWirePropertyNames.CreationTimeUtc},
            {MessageSystemPropertyNames.ContentType, IotHubWirePropertyNames.ContentType},
            {MessageSystemPropertyNames.ContentEncoding, IotHubWirePropertyNames.ContentEncoding},
            {MessageSystemPropertyNames.Operation, MessageSystemPropertyNames.Operation},
            {MessageSystemPropertyNames.Ack, MessageSystemPropertyNames.Ack},
            {MessageSystemPropertyNames.OutputName, IotHubWirePropertyNames.OutputName },
            {MessageSystemPropertyNames.DiagId, IotHubWirePropertyNames.MqttDiagIdKey},
            {MessageSystemPropertyNames.DiagCorrelationContext, IotHubWirePropertyNames.MqttDiagCorrelationContextKey},
            {MessageSystemPropertyNames.InterfaceId, IotHubWirePropertyNames.InterfaceId},
            {MessageSystemPropertyNames.ComponentName,IotHubWirePropertyNames.ComponentName }
        };

        internal MqttTransportHandler(PipelineContext context, IotHubClientMqttSettings settings)
            : base(context, settings)
        {
            _mqttTransportSettings = settings;
            _deviceId = context.IotHubConnectionCredentials.DeviceId;
            _moduleId = context.IotHubConnectionCredentials.ModuleId;

            _methodListener = context.MethodCallback;
            _deviceMessageReceivedListener = context.DeviceEventCallback;
            _moduleMessageReceivedListener = context.ModuleEventCallback;
            _onDesiredStatePatchListener = context.DesiredPropertyUpdateCallback;

            _deviceToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceToCloudMessagesTopicFormat, _deviceId);
            _moduleToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, ModuleToCloudMessagesTopicFormat, _deviceId, _moduleId);
            _deviceBoundMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicFormat, _deviceId);
            _moduleEventMessageTopic = string.Format(CultureInfo.InvariantCulture, ModuleEventMessageTopicFormat, _deviceId, _moduleId);
            _edgeModuleInputEventsTopic = string.Format(CultureInfo.InvariantCulture, EdgeModuleInputEventsTopicFormat, _deviceId, _moduleId);

            _modelId = context.ModelId;
            _productInfo = context.ProductInfo;

            var mqttFactory = new MqttFactory(new MqttLogger());

            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            _hostName = context.IotHubConnectionCredentials.IotHubHostName;
            _connectionCredentials = context.IotHubConnectionCredentials;

            if (_mqttTransportSettings.Protocol == IotHubClientTransportProtocol.WebSocket)
            {
                var uri = "wss://" + _hostName + "/$iothub/websocket";
                _mqttClientOptionsBuilder.WithWebSocketServer(uri);

                IWebProxy proxy = _transportSettings.Proxy;
                if (proxy != null)
                {
                    var serviceUri = new Uri(uri);
                    var proxyUri = _transportSettings.Proxy.GetProxy(serviceUri);

                    if (proxy.Credentials != null)
                    {
                        NetworkCredential credentials = proxy.Credentials.GetCredential(serviceUri, "Basic");
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
            else
            {
                // "ssl://" prefix is not needed here
                var uri = _hostName;
                _mqttClientOptionsBuilder.WithTcpServer(uri, ProtocolGatewayPort);
            }

            MqttClientOptionsBuilderTlsParameters tlsParameters = new MqttClientOptionsBuilderTlsParameters();

            List<X509Certificate> certs = _connectionCredentials.Certificate == null
                ? new List<X509Certificate>(0)
                : new List<X509Certificate> { _connectionCredentials.Certificate };

            tlsParameters.Certificates = certs;

            if (_mqttTransportSettings?.RemoteCertificateValidationCallback != null)
            {
                tlsParameters.CertificateValidationHandler = certificateValidationHandler;
            }

            tlsParameters.UseTls = true;
            tlsParameters.SslProtocol = _mqttTransportSettings.SslProtocols;
            _mqttClientOptionsBuilder.WithTls(tlsParameters);

            _mqttClientOptionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311); // 3.1.1

            _mqttClientOptionsBuilder.WithCleanSession(_mqttTransportSettings.CleanSession);

            _mqttClientOptionsBuilder.WithKeepAlivePeriod(_mqttTransportSettings.KeepAlive);

            if (_mqttTransportSettings.HasWill && _mqttTransportSettings.WillMessage != null)
            {
                _mqttClientOptionsBuilder.WithWillTopic(_deviceToCloudMessagesTopic);
                _mqttClientOptionsBuilder.WithWillPayload(_mqttTransportSettings.WillMessage.Payload);

                if (_mqttTransportSettings.WillMessage.QualityOfService == QualityOfService.AtMostOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
                }
                else if (_mqttTransportSettings.WillMessage.QualityOfService == QualityOfService.AtLeastOnce)
                {
                    _mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
                }
            }

            _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            _mqttClient.DisconnectedAsync += HandleDisconnection;

            publishingQualityOfService = _mqttTransportSettings.PublishToServerQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            receivingQualityOfService = _mqttTransportSettings.ReceivingQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;
        }

        private bool certificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            return _mqttTransportSettings.RemoteCertificateValidationCallback.Invoke(
                _mqttClient,
                args.Certificate,
                args.Chain,
                args.SslPolicyErrors);
        }

        #region Client operations

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            string clientId = _moduleId == null ? _deviceId : _deviceId + "/" + _moduleId;
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
            }
            catch (MqttConnectingFailedException cfe)
            {
                var connectCode = cfe.ResultCode;
                switch (connectCode)
                {
                    case MqttClientConnectResultCode.BadUserNameOrPassword:
                    case MqttClientConnectResultCode.NotAuthorized:
                    case MqttClientConnectResultCode.ClientIdentifierNotValid:
                        throw new IotHubClientException("Failed to open the MQTT connection due to incorrect or unauthorized credentials", cfe, false, IotHubStatusCode.Unauthorized);
                    case MqttClientConnectResultCode.UnsupportedProtocolVersion:
                        // Should never happen since the protocol version (3.1.1) is hardcoded
                        throw new IotHubClientException("Failed to open the MQTT connection due to an unsupported MQTT version", cfe);
                    case MqttClientConnectResultCode.ServerUnavailable:
                        throw new IotHubClientException("MQTT connection rejected because the server was unavailable", cfe, true, IotHubStatusCode.ServerBusy);
                    default:
                        // MQTT 3.1.1 only supports the above connect return codes, so this default case
                        // should never happen. For more details, see the MQTT 3.1.1 specification section "3.2.2.3 Connect Return code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html
                        // MQTT 5 supports a larger set of connect codes. See the MQTT 5.0 specification section "3.2.2.2 Connect Reason Code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901074
                        throw new IotHubClientException("Failed to open the MQTT connection", cfe);
                }
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Topic depends on if the client is a module client or a device client
                string baseTopicName = _moduleId == null ? _deviceToCloudMessagesTopic : _moduleToCloudMessagesTopic;
                string TopicName = PopulateMessagePropertiesFromMessage(baseTopicName, message);

                if (message.HasPayload && message.Payload.Length > MaxMessageSize)
                {
                    throw new InvalidOperationException($"Message size ({message.Payload.Length} bytes) is too big to process. Maximum allowed payload size is {MaxMessageSize}");
                }

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(TopicName)
                    .WithPayload(message.Payload)
                    .WithQualityOfServiceLevel(publishingQualityOfService)
                    .Build();

                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException($"Failed to publish the MQTT packet for message with correlation Id {message.CorrelationId} with reason code {result.ReasonCode}", true);
                }
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to send message", e, true);
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note that this sends all messages at once and then waits for all the acknowledgements. This
            // is the recommended pattern for sending large numbers of messages over an asynchronous
            // protocol like MQTT
            int index = 0;
            Task[] sendTasks = new Task[messages.Count()];
            foreach (Message message in messages)
            {
                sendTasks[index] = SendEventAsync(message, cancellationToken);
                index++;
            }

            // There is no need to check cancellation token here since each task being waited on here
            // has the cancellation token and will finish when the cancellation token is cancelled.
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }

        public override Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            //TODO stubbing this method because we plan on removing it from the device client later.
            Message message = null;
            return Task.FromResult(message);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await SubscribeAsync(DirectMethodsRequestTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to enable receiving direct methods", e, true);
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UnsubscribeAsync(DirectMethodsRequestTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to disable receiving direct methods", e, true);
            }
        }

        public override async Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            var topic = DirectMethodsResponseTopicFormat.FormatInvariant(methodResponse.Status, methodResponse.RequestId);

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(methodResponse.Payload)
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .Build();

            try
            {
                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException($"Failed to send direct method response with reason code {result.ReasonCode}", true);
                }
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to send direct method response", e, true);
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                await SubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to enable receiving messages", e, true);
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UnsubscribeAsync(_deviceBoundMessagesTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to disable receiving messages", e, true);
            }
        }

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                if (isAnEdgeModule)
                {
                    await SubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SubscribeAsync(_moduleEventMessageTopic, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to enable receiving messages", e, true);
            }
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                if (isAnEdgeModule)
                {
                    await UnsubscribeAsync(_edgeModuleInputEventsTopic, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await UnsubscribeAsync(_moduleEventMessageTopic, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to disable receiving messages", e, true);
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await SubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to enable receiving twin patches", e, true);
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UnsubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to disable receiving twin patches", e, true);
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (!_isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken).ConfigureAwait(false);
                _isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(TwinGetTopicFormat.FormatInvariant(requestId))
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .Build();

            try
            {
                // Note the request as "in progress" before actually sending it so that no matter how quickly the service
                // responds, this layer can correlate the request.
                var taskCompletionSource = new TaskCompletionSource<GetTwinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                _getTwinResponseCompletions[requestId] = taskCompletionSource;

                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException($"Failed to publish the mqtt packet for getting this client's twin with reason code {result.ReasonCode}", true);
                }

                if (Logging.IsEnabled)
                    Logging.Info($"Sent get twin request. Waiting on service response with request id {requestId}");

                // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                GetTwinResponse getTwinResponse =
                    await GetTaskCompletionSourceResultAsync(
                        taskCompletionSource,
                        cancellationToken).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Received get twin response for request id {requestId} with status {getTwinResponse.Status}.");

                if (getTwinResponse.Status != 200)
                {
                    throw new IotHubClientException(getTwinResponse.Message);
                }

                return getTwinResponse.Twin;
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to get twin", e, true);
            }
            finally
            {
                // No matter what, remove the requestId from this dictionary since no thread will be waiting for it anymore
                _getTwinResponseCompletions.TryRemove(requestId, out TaskCompletionSource<GetTwinResponse> _);
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (!_isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken).ConfigureAwait(false);
                _isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();
            string topic = string.Format(TwinReportedPropertiesPatchTopicFormat, requestId);

            string body = JsonConvert.SerializeObject(reportedProperties);

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .WithPayload(Encoding.UTF8.GetBytes(body))
                .Build();

            try
            {
                // Note the request as "in progress" before actually sending it so that no matter how quickly the service
                // responds, this layer can correlate the request.
                var taskCompletionSource = new TaskCompletionSource<PatchTwinResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                _reportedPropertyUpdateResponseCompletions[requestId] = taskCompletionSource;

                MqttClientPublishResult result = await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new IotHubClientException($"Failed to publish the mqtt packet for patching this client's twin with reason code {result.ReasonCode}", true);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Sent twin patch request with request id {requestId}. Now waiting for the service response.");

                // Wait until IoT hub sends a message to this client with the response to this patch twin request.
                PatchTwinResponse patchTwinResponse =
                    await GetTaskCompletionSourceResultAsync(
                        taskCompletionSource,
                        cancellationToken)
                        .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Received twin patch response for request id {requestId} with status {patchTwinResponse.Status}.");

                if (patchTwinResponse.Status != 204)
                {
                    throw new IotHubClientException(patchTwinResponse.Message);
                }

                //TODO new twin version should be returned here, but API surface doesn't currently allow it
                //return patchTwinResponse.Version;
            }
            catch (Exception e) when (e is not IotHubClientException)
            {
                throw new IotHubClientException("Failed to send twin patch", e, true);
            }
            finally
            {
                // No matter what, remove the requestId from this dictionary since no thread will be waiting for it anymore
                _reportedPropertyUpdateResponseCompletions.TryRemove(requestId, out TaskCompletionSource<PatchTwinResponse> _);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _mqttClient?.Dispose();
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            OnTransportClosedGracefully();
            MqttClientDisconnectOptions disconnectOptions = new MqttClientDisconnectOptions();

            try
            {
                await _mqttClient.DisconnectAsync(disconnectOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Deliberately not rethrowing the exception because this is a "best effort" close.
                // The service may not have acknowledged that the client closed the connection, but
                // all local resources have been closed. The service will eventually realize the
                // connection is closed in cases like these.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Failed to gracefully close the MQTT client {e}");
            }
        }

        #endregion Client operations

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
            string fullTopic = topic + "#";

            MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(fullTopic, receivingQualityOfService)
                .Build();

            MqttClientSubscribeResult subscribeResults = await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

            if (subscribeResults == null || subscribeResults.Items == null)
            {
                throw new IotHubClientException("Failed to subscribe to topic " + fullTopic, true);
            }

            MqttClientSubscribeResultItem subscribeResult = subscribeResults.Items.FirstOrDefault();

            if (!subscribeResult.TopicFilter.Topic.Equals(fullTopic))
            {
                throw new IotHubClientException("Received unexpected subscription to topic " + subscribeResult.TopicFilter.Topic, true);
            }
        }

        private async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken)
        {
            MqttClientUnsubscribeOptions unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

            MqttClientUnsubscribeResult unsubscribeResults = await _mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken).ConfigureAwait(false);

            if (unsubscribeResults == null || unsubscribeResults.Items == null || unsubscribeResults.Items.Count != 1)
            {
                throw new IotHubClientException("Failed to unsubscribe to topic " + topic, true);
            }

            MqttClientUnsubscribeResultItem unsubscribeResult = unsubscribeResults.Items.FirstOrDefault();
            if (!unsubscribeResult.TopicFilter.Equals(topic))
            {
                throw new IotHubClientException("Received unexpected unsubscription from topic " + unsubscribeResult.TopicFilter, true);
            }

            if (unsubscribeResult.ResultCode != MqttClientUnsubscribeResultCode.Success)
            {
                throw new IotHubClientException("Failed to unsubscribe from topic " + topic + " with reason " + unsubscribeResult.ResultCode, true);
            }
        }

        private Task HandleDisconnection(MqttClientDisconnectedEventArgs disconnectedEventArgs)
        {
            if (Logging.IsEnabled)
                Logging.Info($"MQTT connection was lost {disconnectedEventArgs.Exception}");

            if (disconnectedEventArgs.ClientWasConnected)
            {
                OnTransportDisconnected();
            }

            return Task.CompletedTask;
        }

        private async Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = false;
            string topic = receivedEventArgs.ApplicationMessage.Topic;

            if (topic.StartsWith(_deviceBoundMessagesTopic))
            {
                await HandleReceivedCloudToDeviceMessage(receivedEventArgs).ConfigureAwait(false);
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
                await HandleIncomingEventMessage(receivedEventArgs).ConfigureAwait(false);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Received an MQTT message on unexpected topic {topic}. Ignoring message.");
            }
        }

        private async Task HandleReceivedCloudToDeviceMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            var receivedCloudToDeviceMessage = new Message(payload);

            PopulateMessagePropertiesFromMqttMessage(receivedCloudToDeviceMessage, receivedEventArgs.ApplicationMessage);

            if (_deviceMessageReceivedListener != null)
            {
                // We are intentionally not awaiting _deviceMessageReceivedListener callback.
                // This is a user-supplied callback that isn't required to be awaited by us. We can simply invoke it and continue.
                _ = _deviceMessageReceivedListener.Invoke(receivedCloudToDeviceMessage);

                // note that MQTT does not support Abandon or Reject, so always Complete by acknowledging the message like this.
                // Also note that
                try
                {
                    await receivedEventArgs.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // This likely happened because the connection was lost. The service will re-send this message so the user
                    // can acknowledge it on the new connection.
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"Failed to send the acknowledgement for a received cloud to device message {e}"); ;
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
            receivedEventArgs.AutoAcknowledge = true;

            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            var receivedDirectMethod = new Message(payload);

            PopulateMessagePropertiesFromMqttMessage(receivedDirectMethod, receivedEventArgs.ApplicationMessage);

            string[] tokens = Regex.Split(receivedEventArgs.ApplicationMessage.Topic, "/", RegexOptions.Compiled);

            NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(tokens[4]);
            string requestId = queryStringKeyValuePairs.Get("$rid");
            string methodName = tokens[3];

            var methodRequest = new DirectMethodRequest()
            {
                MethodName = methodName,
                RequestId = requestId,
                Payload = Encoding.UTF8.GetString(payload)
            };

            // We are intentionally not awaiting _methodListener callback. The direct method response
            // is handled elsewhere, so we can simply invoke this callback and continue.
            _methodListener.Invoke(methodRequest).ConfigureAwait(false);
        }

        private void HandleReceivedDesiredPropertiesUpdateRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;
            string patch = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
            TwinCollection twinCollection = JsonConvert.DeserializeObject<TwinCollection>(patch);

            _onDesiredStatePatchListener.Invoke(twinCollection);
        }

        private void HandleTwinResponse(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            if (ParseResponseTopic(receivedEventArgs.ApplicationMessage.Topic, out string receivedRequestId, out int status, out int version))
            {
                string payloadString = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload ?? new byte[0]);

                if (_getTwinResponseCompletions.TryRemove(receivedRequestId, out TaskCompletionSource<GetTwinResponse> getTwinCompletion))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to get twin request with request id {receivedRequestId}.");

                    if (status != 200)
                    {
                        var getTwinResponse = new GetTwinResponse
                        {
                            Status = status,
                            Message = payloadString,
                        };

                        getTwinCompletion.SetResult(getTwinResponse);
                    }
                    else
                    {
                        try
                        {
                            Twin twin = new Twin
                            {
                                Properties = JsonConvert.DeserializeObject<TwinProperties>(payloadString),
                            };

                            var getTwinResponse = new GetTwinResponse
                            {
                                Status = status,
                                Twin = twin,
                            };

                            getTwinCompletion.SetResult(getTwinResponse);
                        }
                        catch (JsonReaderException ex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Failed to parse Twin JSON: {ex}. Message body: '{payloadString}'");

                            getTwinCompletion.SetException(ex);
                        }
                    }
                }
                else if (_reportedPropertyUpdateResponseCompletions.TryRemove(receivedRequestId, out TaskCompletionSource<PatchTwinResponse> patchTwinCompletion))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to patch twin request with request id {receivedRequestId}.");

                    // This received message is in response to an update reported properties request.
                    var patchTwinResponse = new PatchTwinResponse()
                    {
                        Status = status,
                        Version = version,
                        Message = payloadString,
                    };

                    patchTwinCompletion.SetResult(patchTwinResponse);
                }
                else
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received response to an unknown twin request with request id {receivedRequestId}. Discarding it.");
                }
            }
        }

        private async Task HandleIncomingEventMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;

            var iotHubMessage = new Message(receivedEventArgs.ApplicationMessage.Payload);

            // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
            // We try to get the endpoint from the topic, if the topic is in the above format.
            string[] tokens = receivedEventArgs.ApplicationMessage.Topic.Split('/');

            // if there is an input name in the topic string, set the system property accordingly
            if (tokens.Length >= 6)
            {
                iotHubMessage.SystemProperties.Add(MessageSystemPropertyNames.InputName, tokens[5]);
            }

            await (_moduleMessageReceivedListener?.Invoke(iotHubMessage)).ConfigureAwait(false);
        }

        public void PopulateMessagePropertiesFromMqttMessage(Message message, MqttApplicationMessage mqttMessage)
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
            if (systemProperty is string)
            {
                return (string)systemProperty;
            }
            if (systemProperty is DateTime)
            {
                return ((DateTime)systemProperty).ToString("o", CultureInfo.InvariantCulture);
            }
            return systemProperty?.ToString();
        }

        private static object ConvertToSystemProperty(KeyValuePair<string, string> property)
        {
            if (string.IsNullOrEmpty(property.Value))
            {
                return property.Value;
            }

            if (property.Key == IotHubWirePropertyNames.AbsoluteExpiryTime ||
                property.Key == IotHubWirePropertyNames.CreationTimeUtc)
            {
                return DateTime.ParseExact(property.Value, "o", CultureInfo.InvariantCulture);
            }

            if (property.Key == MessageSystemPropertyNames.Ack)
            {
                return property.Value switch
                {
                    "none" => DeliveryAcknowledgement.None,
                    "negative" => DeliveryAcknowledgement.NegativeOnly,
                    "positive" => DeliveryAcknowledgement.PositiveOnly,
                    "full" => DeliveryAcknowledgement.Full,
                    _ => throw new NotSupportedException($"Unknown value: '{property.Value}'"),
                };
            }

            return property.Value;
        }

        internal static string PopulateMessagePropertiesFromMessage(string topicName, Message message)
        {
            var systemProperties = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> property in message.SystemProperties)
            {
                if (s_fromSystemPropertiesMap.TryGetValue(property.Key, out string propertyName))
                {
                    systemProperties[propertyName] = ConvertFromSystemProperties(property.Value);
                }
            }

            Dictionary<string, string> mergedProperties = systemProperties.Concat(message.Properties)
                .ToLookup(x => x.Key, x => x.Value)
                .ToDictionary(x => x.Key, g => g.First());

            string properties = UrlEncodedDictionarySerializer.Serialize(mergedProperties);

            string msg = properties.Length != 0
                ? topicName.EndsWith("/", StringComparison.Ordinal) ? topicName + properties + "/" : topicName + "/" + properties
                : topicName;

            if (Encoding.UTF8.GetByteCount(msg) > MaxTopicNameLength)
            {
                throw new IotHubClientException($"TopicName for MQTT packet cannot be larger than {MaxTopicNameLength} bytes, " +
                    $"current length is {Encoding.UTF8.GetByteCount(msg)}." +
                    $" The probable cause is the list of message.Properties and/or message.systemProperties is too long.", false, IotHubStatusCode.MessageTooLarge);
            }

            return msg;
        }

        private bool ParseResponseTopic(string topicName, out string rid, out int status, out int version)
        {
            rid = "";
            status = 500;
            version = 0;

            Match match = _twinResponseTopicRegex.Match(topicName);
            if (match.Success)
            {
                status = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);

                NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(match.Groups[2].Value);
                rid = queryStringKeyValuePairs.Get("$rid");

                if (status == 204)
                {
                    // This query string key-value pair is only expected in a successful patch twin response message.
                    // Get twin requests will contain the twin version in the payload instead.
                    version = int.Parse(queryStringKeyValuePairs.Get("$version"));
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the result of the provided task completion source or throws OperationCancelledException if the provided
        /// cancellation token is cancelled beforehand.
        /// </summary>
        /// <typeparam name="T">The type of the result of the task completion source.</typeparam>
        /// <param name="taskCompletionSource">The task completion source to asynchronously wait for the result of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the provided task completion source if it completes before the provided cancellation token is cancelled.</returns>
        /// <exception cref="OperationCanceledException">If the cancellation token is cancelled before the provided task completion source finishes.</exception>
        private static async Task<T> GetTaskCompletionSourceResultAsync<T>(TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken)
        {
            // Note that Task.Delay(-1, cancellationToken) effectively waits until the cancellation token is cancelled. The -1 value
            // just means that the task is allowed to run indefinitely.
            Task finishedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);

            // If the finished task is not the cancellation token
            if (finishedTask is Task<T>)
            {
                return await ((Task<T>)finishedTask).ConfigureAwait(false);
            }

            // Otherwise throw operation cancelled exception since the cancellation token was cancelled before the task finished.
            throw new OperationCanceledException();
        }
    }
}
