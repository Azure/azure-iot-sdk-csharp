// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices.Client.Common;
using Microsoft.Azure.Devices.Client.Exceptions;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class MqttTransportHandler : TransportHandler
    {
        private readonly MqttTransportSettings mqttTransportSettings;

        private const int ProtocolGatewayPort = 8883;
        private const int MaxMessageSize = 256 * 1024;
        private const int MaxTopicNameLength = 65535;

        private const string DeviceToCloudMessagesTopicFormat = "devices/{0}/messages/events/";
        private const string ModuleToCloudMessagesTopicFormat = "devices/{0}/modules/{1}/messages/events/";
        private string deviceToCloudMessagesTopic;
        private string moduleToCloudMessagesTopic;

        // Topic names for receiving cloud-to-device messages.

        private const string DeviceBoundMessagesTopicFormat = "devices/{0}/messages/devicebound/";
        private string deviceBoundMessagesTopic;

        // Topic names for enabling input events on edge Modules.

        private const string EdgeModuleInputEventsTopicFormat = "devices/{0}/modules/{1}/inputs/";
        private string edgeModuleInputEventsTopic;

        // Topic names for enabling events on non-edge Modules.

        private const string ModuleEventMessageTopicFormat = "devices/{0}/modules/{1}/";
        private string moduleEventMessageTopic;

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

        private const string DeviceClientTypeParam = "DeviceClientType";

        internal IMqttClient mqttClient;
        internal MqttClientOptions mqttClientOptions;
        private MqttClientOptionsBuilder mqttClientOptionsBuilder;

        private MqttQualityOfServiceLevel publishingQualityOfService;
        private MqttQualityOfServiceLevel receivingQualityOfService;

        private readonly Func<MethodRequestInternal, Task> _methodListener;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly Func<string, Message, Task> _moduleMessageReceivedListener;
        private readonly Func<Message, Task> _deviceMessageReceivedListener;

        private readonly ConcurrentDictionary<string, MqttApplicationMessageReceivedEventArgs> messagesToAcknowledge = new ConcurrentDictionary<string, MqttApplicationMessageReceivedEventArgs>();

        private readonly ConcurrentDictionary<string, GetTwinResponse> getTwinResponses = new ConcurrentDictionary<string, GetTwinResponse>();
        private SemaphoreSlim _getTwinSemaphore = new SemaphoreSlim(0);

        private readonly ConcurrentDictionary<string, PatchTwinResponse> reportedPropertyUpdateResponses = new ConcurrentDictionary<string, PatchTwinResponse>();
        private SemaphoreSlim _reportedPropertyUpdateResponsesSemaphore = new SemaphoreSlim(0);

        private readonly List<string> inProgressUpdateReportedPropertiesRequests = new List<string>();
        private readonly List<string> inProgressGetTwinRequests = new List<string>();

        private bool isSubscribedToDesiredPropertyPatches;
        private bool isSubscribedToTwinResponses;

        private const string ModelIdParam = "model-id";
        private const string AuthChainParam = "auth-chain";

        private readonly string deviceId;
        private readonly string moduleId;
        private readonly string hostName;
        private readonly ClientOptions clientOptions;
        private readonly IotHubConnectionString _connectionString;
        private bool isSymmetricKeyAuthenticated;
        private readonly ProductInfo productInfo;

        // Used to correlate back to a received message when the user wants to acknowledge it. This is not a value
        // that is sent over the wire, so we increment this value locally instead.
        private int nextLockToken;

        private static class IotHubWirePropertyNames
        {
            public const string AbsoluteExpiryTime = "$.exp";
            public const string CorrelationId = "$.cid";
            public const string MessageId = "$.mid";
            public const string To = "$.to";
            public const string UserId = "$.uid";
            public const string OutputName = "$.on";
            public const string MessageSchema = "$.schema";
            public const string CreationTimeUtc = "$.ctime";
            public const string ContentType = "$.ct";
            public const string ContentEncoding = "$.ce";
            public const string ConnectionDeviceId = "$.cdid";
            public const string ConnectionModuleId = "$.cmid";
            public const string MqttDiagIdKey = "$.diagid";
            public const string MqttDiagCorrelationContextKey = "$.diagctx";
            public const string InterfaceId = "$.ifid";
            public const string ComponentName = "$.sub";
        }

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

        internal MqttTransportHandler(
            PipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null)
            : this(context, iotHubConnectionString, settings)
        {
            _methodListener = onMethodCallback;
            _deviceMessageReceivedListener = onDeviceMessageReceivedCallback;
            _moduleMessageReceivedListener = onModuleMessageReceivedCallback;
            _onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;
        }

        internal MqttTransportHandler(
            PipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings)
            : base(context, settings)
        {
            mqttTransportSettings = settings;
            _connectionString = iotHubConnectionString;
            deviceId = iotHubConnectionString.DeviceId;
            moduleId = iotHubConnectionString.ModuleId;

            deviceToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceToCloudMessagesTopicFormat, deviceId);
            moduleToCloudMessagesTopic = string.Format(CultureInfo.InvariantCulture, ModuleToCloudMessagesTopicFormat, deviceId, moduleId);
            deviceBoundMessagesTopic = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicFormat, deviceId);
            moduleEventMessageTopic = string.Format(CultureInfo.InvariantCulture, ModuleEventMessageTopicFormat, deviceId, moduleId);
            edgeModuleInputEventsTopic = string.Format(CultureInfo.InvariantCulture, EdgeModuleInputEventsTopicFormat, deviceId, moduleId);

            var mqttFactory = new MqttFactory(new MqttLogger());

            mqttClient = mqttFactory.CreateMqttClient();
            mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            hostName = iotHubConnectionString.HostName;
            if (iotHubConnectionString.SharedAccessKey != null || iotHubConnectionString.TokenRefresher != null)
            {
                isSymmetricKeyAuthenticated = true;
            }

            productInfo = context.ProductInfo;
            clientOptions = context.ClientOptions;

            if (settings.GetTransportType() == TransportType.Mqtt_WebSocket_Only)
            {
                var uri = "wss://" + hostName + "/$iothub/websocket";
                mqttClientOptionsBuilder.WithWebSocketServer(uri);

                IWebProxy proxy = _transportSettings.Proxy;
                if (proxy != null && !(proxy is DefaultWebProxySettings))
                {
                    var serviceUri = new Uri(uri);
                    var proxyUri = _transportSettings.Proxy.GetProxy(serviceUri);

                    if (proxy.Credentials != null)
                    {
                        NetworkCredential credentials = proxy.Credentials.GetCredential(serviceUri, "Basic");
                        string username = credentials.UserName;
                        string password = credentials.Password;
                        mqttClientOptionsBuilder.WithProxy(proxyUri.AbsoluteUri, username, password);
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
                mqttClientOptionsBuilder.WithTcpServer(uri, ProtocolGatewayPort);
            }

            MqttClientOptionsBuilderTlsParameters tlsParameters = new MqttClientOptionsBuilderTlsParameters();

            List<X509Certificate> certs = settings.ClientCertificate == null
                ? new List<X509Certificate>(0)
                : new List<X509Certificate> { settings.ClientCertificate };

            tlsParameters.Certificates = certs;

            if (mqttTransportSettings?.RemoteCertificateValidationCallback != null)
            {
                tlsParameters.CertificateValidationHandler = certificateValidationHandler;
            }

            tlsParameters.UseTls = true;
            tlsParameters.SslProtocol = TlsVersions.Instance.Preferred;
            mqttClientOptionsBuilder.WithTls(tlsParameters);

            mqttClientOptionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311); // 3.1.1

            mqttClientOptionsBuilder.WithCleanSession(settings.CleanSession);

            mqttClientOptionsBuilder.WithKeepAlivePeriod(settings.KeepAlive);

            if (settings.HasWill && settings.WillMessage != null)
            {
                mqttClientOptionsBuilder.WithWillTopic(deviceToCloudMessagesTopic);
                mqttClientOptionsBuilder.WithWillPayload(settings.WillMessage.Payload);

                if (settings.WillMessage.QualityOfService == QualityOfService.AtMostOnce)
                {
                    mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
                }
                else if (settings.WillMessage.QualityOfService == QualityOfService.AtLeastOnce)
                {
                    mqttClientOptionsBuilder.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
                }
            }

            mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            mqttClient.DisconnectedAsync += HandleDisconnection;

            publishingQualityOfService = settings.PublishToServerQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;

            receivingQualityOfService = settings.ReceivingQoS == QualityOfService.AtLeastOnce
                ? MqttQualityOfServiceLevel.AtLeastOnce : MqttQualityOfServiceLevel.AtMostOnce;
        }

        private bool certificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            return mqttTransportSettings.RemoteCertificateValidationCallback.Invoke(
                mqttClient,
                args.Certificate,
                args.Chain,
                args.SslPolicyErrors);
        }

        #region Client operations

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            string clientId = moduleId == null ? deviceId : deviceId + "/" + moduleId;
            mqttClientOptionsBuilder.WithClientId(clientId);

            string username = $"{hostName}/{clientId}/?{ClientApiVersionHelper.ApiVersionQueryStringLatest}&{DeviceClientTypeParam}={Uri.EscapeDataString(productInfo.ToString())}";

            if (!string.IsNullOrWhiteSpace(clientOptions?.ModelId))
            {
                username += $"&{ModelIdParam}={Uri.EscapeDataString(clientOptions.ModelId)}";
            }

            if (!string.IsNullOrWhiteSpace(mqttTransportSettings?.AuthenticationChain))
            {
                username += $"&{AuthChainParam}={Uri.EscapeDataString(mqttTransportSettings.AuthenticationChain)}";
            }

            if (isSymmetricKeyAuthenticated)
            {
                // Symmetric key authenticated connections need to set client Id, username, and password
                string password = ((IAuthorizationProvider)_connectionString).GetPasswordAsync().Result;
                mqttClientOptionsBuilder.WithCredentials(username, password);
            }
            else
            {
                // x509 authenticated connections only need to set client Id and username
                mqttClientOptionsBuilder.WithCredentials(username);
            }

            mqttClientOptions = mqttClientOptionsBuilder.Build();

            try
            {
                await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
            }
            catch (MqttConnectingFailedException cfe)
            {
                var connectCode = cfe.ResultCode;
                switch (connectCode)
                {
                    case MqttClientConnectResultCode.BadUserNameOrPassword:
                    case MqttClientConnectResultCode.NotAuthorized:
                    case MqttClientConnectResultCode.ClientIdentifierNotValid:
                        throw new UnauthorizedException("Failed to open the MQTT connection due to incorrect or unauthorized credentials", cfe);
                    case MqttClientConnectResultCode.UnsupportedProtocolVersion:
                        // Should never happen since the protocol version (3.1.1) is hardcoded
                        throw new IotHubCommunicationException("Failed to open the MQTT connection due to an unsupported MQTT version", cfe);
                    case MqttClientConnectResultCode.ServerUnavailable:
                        throw new ServerBusyException("MQTT connection rejected because the server was unavailable", cfe);
                    default:
                        // MQTT 3.1.1 only supports the above connect return codes, so this default case
                        // should never happen. For more details, see the MQTT 3.1.1 specification section "3.2.2.3 Connect Return code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html
                        // MQTT 5 supports a larger set of connect codes. See the MQTT 5.0 specification section "3.2.2.2 Connect Reason Code"
                        // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901074
                        throw new IotHubCommunicationException("Failed to open the MQTT connection", cfe);
                }
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string TopicName;
            if (moduleId == null)
            {
                // sender is a device
                TopicName = PopulateMessagePropertiesFromMessage(deviceToCloudMessagesTopic, message);
            }
            else
            {
                // sender is a module
                TopicName = PopulateMessagePropertiesFromMessage(moduleToCloudMessagesTopic, message);
            }

            Stream payloadStream = message.GetBodyStream();
            long streamLength = payloadStream.Length;
            if (streamLength > MaxMessageSize)
            {
                throw new InvalidOperationException($"Message size ({streamLength} bytes) is too big to process. Maximum allowed payload size is {MaxMessageSize}");
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(TopicName)
                .WithPayload(payloadStream)
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .Build();

            MqttClientPublishResult result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new IotHubCommunicationException($"Failed to publish the mqtt packet for message with correlation id {message.CorrelationId} with reason code {result.ReasonCode}");
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note that this sends all messages at once and then waits for all the acknowledgements. This
            // is the recommended pattern for sending large numbers of messages over an asynchronous
            // protocol like MQTT
            List<Task> sendTasks = new List<Task>();
            foreach (Message message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sendTasks.Add(SendEventAsync(message, cancellationToken));
            }

            await Task.WhenAll(sendTasks);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            await SubscribeAsync(DirectMethodsRequestTopic, cancellationToken);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            await UnsubscribeAsync(DirectMethodsRequestTopic, cancellationToken);
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            var topic = DirectMethodsResponseTopicFormat.FormatInvariant(methodResponse.Status, methodResponse.RequestId);

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(methodResponse.BodyStream)
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .Build();

            MqttClientPublishResult result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new IotHubCommunicationException($"Failed to publish the mqtt packet for direct method response with reason code {result.ReasonCode}");
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await SubscribeAsync(deviceBoundMessagesTopic, cancellationToken);
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await UnsubscribeAsync(deviceBoundMessagesTopic, cancellationToken);
        }

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            if (isAnEdgeModule)
            {
                await SubscribeAsync(edgeModuleInputEventsTopic, cancellationToken);
            }
            else
            {
                await SubscribeAsync(moduleEventMessageTopic, cancellationToken);
            }

            isSubscribedToDesiredPropertyPatches = true;
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            await UnsubscribeAsync(moduleEventMessageTopic, cancellationToken);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (isSubscribedToDesiredPropertyPatches)
            {
                return;
            }

            await SubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken);

            isSubscribedToDesiredPropertyPatches = true;
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            await UnsubscribeAsync(TwinDesiredPropertiesPatchTopic, cancellationToken);

            isSubscribedToDesiredPropertyPatches = false;
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (!isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken);
                isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(TwinGetTopicFormat.FormatInvariant(requestId))
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .Build();

            MqttClientPublishResult result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new IotHubCommunicationException($"Failed to publish the mqtt packet for getting this client's twin with reason code {result.ReasonCode}");
            }

            inProgressGetTwinRequests.Add(requestId);

            while (!getTwinResponses.ContainsKey(requestId))
            {
                // May need to wait multiple times. This semaphore is released each time a get twin
                // request gets a response, but it may not be in response to this particular get twin request.
                _getTwinSemaphore.Wait(cancellationToken);
            }

            getTwinResponses.TryRemove(requestId, out GetTwinResponse getTwinResponse);
            int getTwinStatus = getTwinResponse.Status;

            if (getTwinStatus != 200)
            {
                throw ExceptionHandlingHelper.GetExceptionFromStatusCode(getTwinStatus, "Failed to get twin");
            }

            return getTwinResponse.Twin;
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (!isSubscribedToTwinResponses)
            {
                await SubscribeAsync(TwinResponseTopic, cancellationToken);
                isSubscribedToTwinResponses = true;
            }

            string requestId = Guid.NewGuid().ToString();
            string topic = string.Format(TwinReportedPropertiesPatchTopicFormat, requestId);

            string body = JsonConvert.SerializeObject(reportedProperties);

            MqttApplicationMessage mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(publishingQualityOfService)
                .WithPayload(Encoding.UTF8.GetBytes(body))
                .Build();

            MqttClientPublishResult result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new IotHubCommunicationException($"Failed to publish the mqtt packet for patching this client's twin with reason code {result.ReasonCode}");
            }

            inProgressUpdateReportedPropertiesRequests.Add(requestId);

            while (!reportedPropertyUpdateResponses.ContainsKey(requestId))
            {
                // May need to wait multiple times. This semaphore is released each time a reported
                // properties update request gets a response, but it may not be in response to this
                // particular reported properties request.
                _reportedPropertyUpdateResponsesSemaphore.Wait(cancellationToken);
            }

            reportedPropertyUpdateResponses.TryRemove(requestId, out PatchTwinResponse patchTwinResponse);
            if (patchTwinResponse.Status != 204)
            {
                throw ExceptionHandlingHelper.GetExceptionFromStatusCode(patchTwinResponse.Status, "Failed to send twin patch");
            }

            //TODO new twin version should be returned here, but API surface doesn't currently allow it
            //return patchTwinResponse.Version;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            mqttClient?.Dispose();

            // TODO notify the user for these that they failed? Clear these when closing instead?
            getTwinResponses?.Clear();
            reportedPropertyUpdateResponses?.Clear();
            inProgressGetTwinRequests?.Clear();
            inProgressUpdateReportedPropertiesRequests?.Clear();
            messagesToAcknowledge?.Clear();

            _getTwinSemaphore?.Dispose();
            _reportedPropertyUpdateResponsesSemaphore?.Dispose();

            if (_connectionString?.TokenRefresher != null
                && _connectionString.TokenRefresher.DisposalWithClient)
            {
                _connectionString.TokenRefresher.Dispose();
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            MqttClientDisconnectOptions disconnectOptions = new MqttClientDisconnectOptions();
            await mqttClient.DisconnectAsync(disconnectOptions, cancellationToken);
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

            MqttClientSubscribeResult subscribeResults = await mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);

            if (subscribeResults == null || subscribeResults.Items == null)
            {
                throw new IotHubCommunicationException("Failed to subscribe to topic " + fullTopic);
            }

            // Expecting only 1 result here so the foreach loop should return upon receiving the expected ack
            foreach (MqttClientSubscribeResultItem subscribeResult in subscribeResults.Items)
            {
                if (!subscribeResult.TopicFilter.Topic.Equals(fullTopic))
                {
                    throw new IotHubCommunicationException("Received unexpected subscription to topic " + subscribeResult.TopicFilter.Topic);
                }

                return;
            }

            throw new IotHubCommunicationException("Service did not acknowledge the subscription request for topic " + fullTopic);
        }

        private async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken)
        {
            MqttClientUnsubscribeOptions unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

            MqttClientUnsubscribeResult unsubscribeResults = await mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken);

            if (unsubscribeResults == null || unsubscribeResults.Items == null || unsubscribeResults.Items.Count != 1)
            {
                throw new IotHubCommunicationException("Failed to unsubscribe to topic " + topic);
            }

            // Expecting only 1 result here so the foreach loop should return upon receiving the expected ack
            foreach (MqttClientUnsubscribeResultItem unsubscribeResult in unsubscribeResults.Items)
            {
                if (!unsubscribeResult.TopicFilter.Equals(topic))
                {
                    throw new IotHubCommunicationException("Received unexpected unsubscription from topic " + unsubscribeResult.TopicFilter);
                }

                if (unsubscribeResult.ResultCode != MqttClientUnsubscribeResultCode.Success)
                {
                    throw new IotHubCommunicationException("Failed to unsubscribe from topic " + topic + " with reason " + unsubscribeResult.ResultCode);
                }

                return;
            }

            throw new IotHubCommunicationException("Service did not acknowledge the unsubscribe request for topic " + topic);
        }

        private Task HandleDisconnection(MqttClientDisconnectedEventArgs disconnectedEventArgs)
        {
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
            if (topic.StartsWith(deviceBoundMessagesTopic))
            {
                await HandleReceivedCloudToDeviceMessage(receivedEventArgs);
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
            else if (topic.StartsWith(moduleEventMessageTopic)
                || topic.StartsWith(edgeModuleInputEventsTopic))
            {
                // This works regardless of if the event is on a particular Edge module input or if
                // the module is not an Edge module.
                await HandleIncomingEventMessage(receivedEventArgs);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Received an MQTT message on unexpected topic {topic}. Ignoring message.");
            }
        }

        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The created message is handed to the user and the user application is in charge of disposing the message.")]
        private async Task HandleReceivedCloudToDeviceMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            var receivedCloudToDeviceMessage = new Message(payload);

            PopulateMessagePropertiesFromPacket(receivedCloudToDeviceMessage, receivedEventArgs.ApplicationMessage);

            // Save the received mqtt message instance so that it can be completed later
            messagesToAcknowledge[receivedCloudToDeviceMessage.LockToken] = receivedEventArgs;

            if (_deviceMessageReceivedListener != null)
            {
                // We are intentionally not awaiting _deviceMessageReceivedListener callback.
                // This is a user-supplied callback that isn't required to be awaited by us. We can simply invoke it and continue.
                _ = _deviceMessageReceivedListener.Invoke(receivedCloudToDeviceMessage);
                await receivedEventArgs.AcknowledgeAsync(CancellationToken.None);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "Received a cloud to device message while user's callback for handling them was null. Disposing message.");

                receivedCloudToDeviceMessage.Dispose();
            }
        }

        private void HandleReceivedDirectMethodRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            receivedEventArgs.AutoAcknowledge = true;

            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            using var receivedDirectMethod = new Message(payload);

            PopulateMessagePropertiesFromPacket(receivedDirectMethod, receivedEventArgs.ApplicationMessage);

            string[] tokens = Regex.Split(receivedEventArgs.ApplicationMessage.Topic, "/", RegexOptions.Compiled);

            NameValueCollection queryStringKeyValuePairs = HttpUtility.ParseQueryString(tokens[4]);
            string requestId = queryStringKeyValuePairs.Get("$rid");
            string methodName = tokens[3];

            using var methodRequest = new MethodRequestInternal(methodName, requestId, new MemoryStream(payload));

            // We are intentionally not awaiting _methodListener callback.
            // This is a user-supplied callback that isn't required to be awaited by us. We can simply invoke it and continue.
            _methodListener.Invoke(methodRequest).ConfigureAwait(false);
        }

        private void HandleReceivedDesiredPropertiesUpdateRequest(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

            string patch = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
            TwinCollection twinCollection = JsonConvert.DeserializeObject<TwinCollection>(patch);

            _onDesiredStatePatchListener.Invoke(twinCollection);

            receivedEventArgs.AutoAcknowledge = true;
        }

        private void HandleTwinResponse(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            if (ParseResponseTopic(receivedEventArgs.ApplicationMessage.Topic, out string receivedRequestId, out int status, out int version))
            {
                byte[] payload = receivedEventArgs.ApplicationMessage.Payload;

                if (inProgressGetTwinRequests.Contains(receivedRequestId))
                {
                    // This received message is in response to a GetTwin request
                    inProgressGetTwinRequests.Remove(receivedRequestId);

                    string body = Encoding.UTF8.GetString(payload);

                    if (status != 200)
                    {
                        // Save the status code, but don't throw here. The thread waiting on the
                        // _getTwinSemaphore will check this value and throw if it wasn't successful
                        getTwinResponses[receivedRequestId] = new GetTwinResponse(status);
                    }
                    else
                    {
                        try
                        {
                            Twin twin = new Twin
                            {
                                Properties = JsonConvert.DeserializeObject<TwinProperties>(body),
                            };

                            getTwinResponses[receivedRequestId] = new GetTwinResponse(status, twin);
                        }
                        catch (JsonReaderException ex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Failed to parse Twin JSON: {ex}. Message body: '{body}'");
                        }
                    }

                    _getTwinSemaphore.Release();
                }
                else if (inProgressUpdateReportedPropertiesRequests.Contains(receivedRequestId))
                {
                    // This received message is in response to an update reported properties request.
                    inProgressUpdateReportedPropertiesRequests.Remove(receivedRequestId);
                    reportedPropertyUpdateResponses[receivedRequestId] = new PatchTwinResponse(status, version);
                    _reportedPropertyUpdateResponsesSemaphore.Release();
                }
            }
        }

        private async Task HandleIncomingEventMessage(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            byte[] payload = receivedEventArgs.ApplicationMessage.Payload;
            receivedEventArgs.AutoAcknowledge = true;

            using var iotHubMessage = new Message(payload);

            // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
            // We try to get the endpoint from the topic, if the topic is in the above format.
            string[] tokens = receivedEventArgs.ApplicationMessage.Topic.Split('/');
            string inputName = tokens.Length >= 6 ? tokens[5] : null;

            // Add the endpoint as a SystemProperty
            iotHubMessage.SystemProperties.Add(MessageSystemPropertyNames.InputName, inputName);

            await _moduleMessageReceivedListener?.Invoke(inputName, iotHubMessage);
        }

        public void PopulateMessagePropertiesFromPacket(Message message, MqttApplicationMessage mqttMessage)
        {
            message.LockToken = (++nextLockToken).ToString();

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
                return Utils.ConvertDeliveryAckTypeFromString(property.Value);
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
            string properties = UrlEncodedDictionarySerializer.Serialize(Utils.MergeDictionaries(new IDictionary<string, string>[] { systemProperties, message.Properties }));

            string msg = properties.Length != 0
                ? topicName.EndsWith("/", StringComparison.Ordinal) ? topicName + properties + "/" : topicName + "/" + properties
                : topicName;

            if (Encoding.UTF8.GetByteCount(msg) > MaxTopicNameLength)
            {
                throw new MessageTooLargeException($"TopicName for MQTT packet cannot be larger than {MaxTopicNameLength} bytes, " +
                    $"current length is {Encoding.UTF8.GetByteCount(msg)}." +
                    $" The probable cause is the list of message.Properties and/or message.systemProperties is too long. ");
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
    }
}
