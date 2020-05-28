// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

//using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class MqttTransportHandler : TransportHandler, IMqttIotHubEventHandler
    {
        private const int ProtocolGatewayPort = 8883;
        private const int MaxMessageSize = 256 * 1024;
        private const string ProcessorThreadCountVariableName = "MqttEventsProcessorThreadCount";

        private static readonly int s_generationPrefixLength = Guid.NewGuid().ToString().Length;

        private readonly string _generationId = Guid.NewGuid().ToString();

        private static readonly Lazy<IEventLoopGroup> s_eventLoopGroup = new Lazy<IEventLoopGroup>(GetEventLoopGroup);

        private readonly string _hostName;
        private readonly Func<IPAddress[], int, Task<IChannel>> _channelFactory;
        private readonly Queue<string> _completionQueue;
        private readonly MqttIotHubAdapterFactory _mqttIotHubAdapterFactory;
        private readonly QualityOfService _qos;

        private readonly object _syncRoot = new object();
        private readonly CancellationTokenSource _disconnectAwaitersCancellationSource = new CancellationTokenSource();
        private readonly RetryPolicy _closeRetryPolicy;

        private readonly SemaphoreSlim _receivingSemaphore = new SemaphoreSlim(0);
        private readonly ConcurrentQueue<Message> _messageQueue;

        private readonly TaskCompletionSource _connectCompletion = new TaskCompletionSource();
        private readonly TaskCompletionSource _subscribeCompletionSource = new TaskCompletionSource();
        private Func<Task> _cleanupFunc;
        private IChannel _channel;
        private ExceptionDispatchInfo _fatalException;
        private IPAddress[] _serverAddresses;
        private IWebProxy _webProxy;

        private int _state = (int)TransportState.NotInitialized;
        public TransportState State => (TransportState)Volatile.Read(ref _state);

        // incoming topic names
        private const string MethodPostTopicFilter = "$iothub/methods/POST/#";

        private const string MethodPostTopicPrefix = "$iothub/methods/POST/";
        private const string TwinResponseTopicFilter = "$iothub/twin/res/#";
        private const string TwinResponseTopicPrefix = "$iothub/twin/res/";
        private const string TwinPatchTopicFilter = "$iothub/twin/PATCH/properties/desired/#";
        private const string TwinPatchTopicPrefix = "$iothub/twin/PATCH/properties/desired/";
        private const string ReceiveEventMessagePatternFilter = "devices/{0}/modules/{1}/#";
        private const string ReceiveEventMessagePrefixPattern = "devices/{0}/modules/{1}/";
        private const string DeviceBoundMessagesTopicFilter = "devices/{0}/messages/devicebound/#";
        private const string DeviceBoundMessagesTopicPrefix = "devices/{0}/messages/devicebound/";

        // outgoing topic names
        private const string MethodResponseTopic = "$iothub/methods/res/{0}/?$rid={1}";

        private const string TwinGetTopic = "$iothub/twin/GET/?$rid={0}";
        private const string TwinPatchTopic = "$iothub/twin/PATCH/properties/reported/?$rid={0}";

        // incoming topic regexp
        private const string TwinResponseTopicPattern = @"\$iothub/twin/res/(\d+)/(\?.+)";

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan s_defaultTwinTimeout = TimeSpan.FromSeconds(60);
        private readonly Regex _twinResponseTopicRegex = new Regex(TwinResponseTopicPattern, RegexOptions.Compiled, s_regexTimeoutMilliseconds);

        private readonly Func<MethodRequestInternal, Task> _messageListener;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly Func<string, Message, Task> _messageReceivedListener;
        private Action<Message> _twinResponseEvent;

        private readonly string _receiveEventMessageFilter;
        private readonly string _receiveEventMessagePrefix;

        private readonly string _deviceboundMessageFilter;
        private readonly string _deviceboundMessagePrefix;

        internal MqttTransportHandler(
            IPipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onReceiveCallback = null)
            : this(context, iotHubConnectionString, settings, null)
        {
            _messageListener = onMethodCallback;
            _messageReceivedListener = onReceiveCallback;
            _onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;
        }

        internal MqttTransportHandler(
            IPipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<IPAddress[], int, Task<IChannel>> channelFactory)
            : base(context, settings)
        {
            _mqttIotHubAdapterFactory = new MqttIotHubAdapterFactory(settings);
            _messageQueue = new ConcurrentQueue<Message>();
            _completionQueue = new Queue<string>();

            _serverAddresses = null; // this will be resolved asynchronously in OpenAsync
            _hostName = iotHubConnectionString.HostName;
            _receiveEventMessageFilter = string.Format(CultureInfo.InvariantCulture, ReceiveEventMessagePatternFilter, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);
            _receiveEventMessagePrefix = string.Format(CultureInfo.InvariantCulture, ReceiveEventMessagePrefixPattern, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);

            _deviceboundMessageFilter = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicFilter, iotHubConnectionString.DeviceId);
            _deviceboundMessagePrefix = string.Format(CultureInfo.InvariantCulture, DeviceBoundMessagesTopicPrefix, iotHubConnectionString.DeviceId);

            _qos = settings.PublishToServerQoS;

            _webProxy = settings.Proxy;

            if (channelFactory == null)
            {
                ClientOptions options = context.Get<ClientOptions>();
                switch (settings.GetTransportType())
                {
                    case TransportType.Mqtt_Tcp_Only:
                        _channelFactory = CreateChannelFactory(iotHubConnectionString, settings, context.Get<ProductInfo>(), options);
                        break;

                    case TransportType.Mqtt_WebSocket_Only:
                        _channelFactory = CreateWebSocketChannelFactory(iotHubConnectionString, settings, context.Get<ProductInfo>(), options);
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported Transport Setting {0}".FormatInvariant(settings.GetTransportType()));
                }
            }
            else
            {
                _channelFactory = channelFactory;
            }

            _closeRetryPolicy = new RetryPolicy(new TransientErrorIgnoreStrategy(), 5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public TimeSpan TwinTimeout { get; set; } = s_defaultTwinTimeout;
        public override bool IsUsable => State != TransportState.Closed && State != TransportState.Error;

        #region Client operations

        public override async Task OpenAsync(TimeoutHelper timeoutHelper)
        {
            using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());
            await OpenAsync(cts.Token).ConfigureAwait(false);
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                cancellationToken.ThrowIfCancellationRequested();

                EnsureValidState(throwIfNotOpen: false);

                await OpenAsyncInternal(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(OpenAsync));
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(SendEventAsync));
                cancellationToken.ThrowIfCancellationRequested();

                EnsureValidState();
                Debug.Assert(_channel != null);

                await _channel.WriteAndFlushAsync(message).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            foreach (Message message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SendEventAsync(message, cancellationToken).ConfigureAwait(true);
            }
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Message message = null;

            EnsureValidState();

            if (State != TransportState.Receiving)
            {
                await SubscribeCloudToDeviceMessagesAsync().ConfigureAwait(true);
            }

            // -1 millisecond represents for SemaphoreSlim to wait indefinitely
            bool hasMessage = await ReceiveMessageArrivalAsync(TimeSpan.FromMilliseconds(-1), cancellationToken).ConfigureAwait(true);
            message = ProcessMessage(message, hasMessage);

            return message;
        }

        public override async Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeoutHelper, timeoutHelper.GetRemainingTime(), $"{nameof(ReceiveAsync)}");

            Message message = null;

            EnsureValidState();

            if (State != TransportState.Receiving)
            {
                await SubscribeCloudToDeviceMessagesAsync().ConfigureAwait(true);
            }

            TimeSpan timeout = timeoutHelper.GetRemainingTime();
            using var cts = new CancellationTokenSource(timeout);
            bool hasMessage = await ReceiveMessageArrivalAsync(timeout, cts.Token).ConfigureAwait(true);
            message = ProcessMessage(message, hasMessage);

            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper, timeoutHelper.GetRemainingTime(), $"{nameof(ReceiveAsync)}");

            return message;
        }

        private Message ProcessMessage(Message message, bool hasMessage)
        {
            if (hasMessage)
            {
                lock (_syncRoot)
                {
                    _messageQueue.TryDequeue(out message);
                    message.LockToken = message.LockToken;
                    if (_qos == QualityOfService.AtLeastOnce)
                    {
                        _completionQueue.Enqueue(message.LockToken);
                    }

                    message.LockToken = _generationId + message.LockToken;
                }
            }

            return message;
        }

        private async Task<bool> ReceiveMessageArrivalAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var disconnectToken = _disconnectAwaitersCancellationSource.Token;
            EnsureValidState();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disconnectToken);
            return await _receivingSemaphore.WaitAsync(timeout, linkedCts.Token).ConfigureAwait(true);
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            if (_qos == QualityOfService.AtMostOnce)
            {
                throw new IotHubException("Complete is not allowed for QoS 0.", isTransient: false);
            }

            Task completeOperationCompletion;
            lock (_syncRoot)
            {
                if (!lockToken.StartsWith(_generationId, StringComparison.InvariantCulture))
                {
                    throw new IotHubException(
                        "Lock token is stale or never existed. The message will be redelivered, please discard this lock token and do not retry operation.",
                        isTransient: false);
                }

                if (_completionQueue.Count == 0)
                {
                    throw new IotHubException("Unknown lock token.", isTransient: false);
                }

                string actualLockToken = _completionQueue.Peek();
                if (lockToken.IndexOf(actualLockToken, s_generationPrefixLength, StringComparison.Ordinal) != s_generationPrefixLength ||
                    lockToken.Length != actualLockToken.Length + s_generationPrefixLength)
                {
                    throw new IotHubException(
                        $"Client MUST send PUBACK packets in the order in which the corresponding PUBLISH packets were received (QoS 1 messages) per [MQTT-4.6.0-2]. Expected lock token: '{actualLockToken}'; actual lock token: '{lockToken}'.",
                        isTransient: false);
                }

                _completionQueue.Dequeue();
                completeOperationCompletion = _channel.WriteAndFlushAsync(actualLockToken);
            }

            await completeOperationCompletion.ConfigureAwait(true);
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            base.Dispose(disposing);
            if (disposing)
            {
                if (TryStop())
                {
                    Cleanup();
                }

                _disconnectAwaitersCancellationSource.Dispose();
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, "", $"{nameof(MqttTransportHandler)}.{nameof(CloseAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                if (TryStop())
                {
                    OnTransportClosedGracefully();

                    await _closeRetryPolicy.ExecuteAsync(CleanupAsync, cancellationToken).ConfigureAwait(true);
                }
                else
                {
                    if (State == TransportState.Error)
                    {
                        _fatalException.Throw();
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, "", $"{nameof(MqttTransportHandler)}.{nameof(CloseAsync)}");
            }
        }

        #endregion Client operations

        #region MQTT callbacks

        public void OnConnected()
        {
            if (TryStateTransition(TransportState.Opening, TransportState.Open))
            {
                _connectCompletion.TryComplete();
            }
        }

        private async Task HandleIncomingTwinPatch(Message message)
        {
            try
            {
                if (_onDesiredStatePatchListener != null)
                {
                    using var reader = new StreamReader(message.GetBodyStream(), System.Text.Encoding.UTF8);
                    string patch = reader.ReadToEnd();
                    var props = JsonConvert.DeserializeObject<TwinCollection>(patch);
                    await Task.Run(() => _onDesiredStatePatchListener(props)).ConfigureAwait(true);
                }
            }
            finally
            {
                message.Dispose();
            }
        }

        private async Task HandleIncomingMethodPost(Message message)
        {
            try
            {
                string[] tokens = Regex.Split(message.MqttTopicName, "/", RegexOptions.Compiled, s_regexTimeoutMilliseconds);

                using var mr = new MethodRequestInternal(tokens[3], tokens[4].Substring(6), message.GetBodyStream(), CancellationToken.None);
                await Task.Run(() => _messageListener(mr)).ConfigureAwait(true);
            }
            finally
            {
                message.Dispose();
            }
        }

        public async void OnMessageReceived(Message message)
        {
            // Added Try-Catch to avoid unknown thread exception
            // after running for more than 24 hours
            try
            {
                if ((State & TransportState.Open) == TransportState.Open)
                {
                    string topic = message.MqttTopicName;
                    if (topic.StartsWith(TwinResponseTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        _twinResponseEvent(message);
                    }
                    else if (topic.StartsWith(TwinPatchTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingTwinPatch(message).ConfigureAwait(true);
                    }
                    else if (topic.StartsWith(MethodPostTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingMethodPost(message).ConfigureAwait(true);
                    }
                    else if (topic.StartsWith(_receiveEventMessagePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingEventMessage(message).ConfigureAwait(true);
                    }
                    else if (topic.StartsWith(_deviceboundMessagePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        _messageQueue.Enqueue(message);
                        _receivingSemaphore.Release();
                    }
                    else
                    {
                        if (Logging.IsEnabled) Logging.Error(this, "Recevied mqtt message on an unrecognized topic, ignoring message. Topic: " + topic);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private async Task HandleIncomingEventMessage(Message message)
        {
            try
            {
                // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
                // We try to get the endpoint from the topic, if the topic is in the above format.
                string[] tokens = message.MqttTopicName.Split('/');
                string inputName = tokens.Length >= 6 ? tokens[5] : null;

                // Add the endpoint as a SystemProperty
                message.SystemProperties.Add(MessageSystemPropertyNames.InputName, inputName);

                if (_qos == QualityOfService.AtLeastOnce)
                {
                    lock (_syncRoot)
                    {
                        _completionQueue.Enqueue(message.LockToken);
                    }
                }
                message.LockToken = _generationId + message.LockToken;
                await (_messageReceivedListener?.Invoke(inputName, message) ?? TaskHelpers.CompletedTask).ConfigureAwait(true);
            }
            finally
            {
                message.Dispose();
            }
        }

        public async void OnError(Exception exception)
        {
            try
            {
                TransportState previousState = MoveToStateIfPossible(TransportState.Error, TransportState.Closed);
                switch (previousState)
                {
                    case TransportState.Error:
                    case TransportState.Closed:
                        return;

                    case TransportState.NotInitialized:
                    case TransportState.Opening:
                        _fatalException = ExceptionDispatchInfo.Capture(exception);
                        _connectCompletion.TrySetException(exception);
                        _subscribeCompletionSource.TrySetException(exception);
                        break;

                    case TransportState.Open:
                    case TransportState.Subscribing:
                        _fatalException = ExceptionDispatchInfo.Capture(exception);
                        _subscribeCompletionSource.TrySetException(exception);
                        OnTransportDisconnected();
                        break;

                    case TransportState.Receiving:
                        _fatalException = ExceptionDispatchInfo.Capture(exception);
                        _disconnectAwaitersCancellationSource.Cancel();
                        OnTransportDisconnected();
                        break;

                    default:
                        Debug.Fail($"Unknown transport state: {previousState}");
                        throw new InvalidOperationException();
                }

                await _closeRetryPolicy.ExecuteAsync(CleanupAsync).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (Logging.IsEnabled) Logging.Error(this, ex.ToString(), nameof(OnError));
            }
        }

        private TransportState MoveToStateIfPossible(TransportState destination, TransportState illegalStates)
        {
            TransportState previousState = State;
            do
            {
                if ((previousState & illegalStates) > 0)
                {
                    return previousState;
                }
                TransportState prevState;
                if ((prevState = (TransportState)Interlocked.CompareExchange(ref _state, (int)destination, (int)previousState)) == previousState)
                {
                    return prevState;
                }
                previousState = prevState;
            }
            while (true);
        }

        #endregion MQTT callbacks

        private async Task OpenAsyncInternal(CancellationToken cancellationToken)
        {
            if (IsProxyConfigured())
            {
                //No need to do a DNS lookup since we have the proxy address already
                _serverAddresses = new IPAddress[0];
            }
            else
            {
#if NET451
                _serverAddresses = Dns.GetHostEntry(_hostName).AddressList;
#else
                _serverAddresses = await Dns.GetHostAddressesAsync(_hostName).ConfigureAwait(true);
#endif
            }

            if (TryStateTransition(TransportState.NotInitialized, TransportState.Opening))
            {
                try
                {
                    _channel = await _channelFactory(_serverAddresses, ProtocolGatewayPort).ConfigureAwait(true);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    OnError(ex);
                    throw;
                }

                ScheduleCleanup(async () =>
                {
                    _disconnectAwaitersCancellationSource.Cancel();
                    if (_channel == null)
                    {
                        return;
                    }
                    if (_channel.Active)
                    {
                        await _channel.WriteAsync(DisconnectPacket.Instance).ConfigureAwait(true);
                    }
                    if (_channel.Open)
                    {
                        await _channel.CloseAsync().ConfigureAwait(true);
                    }
                });
            }

            await _connectCompletion.Task.ConfigureAwait(true);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_031: `OpenAsync` shall subscribe using the '$iothub/twin/res/#' topic filter
            await SubscribeTwinResponsesAsync().ConfigureAwait(true);
        }

        private bool TryStop()
        {
            TransportState previousState = MoveToStateIfPossible(TransportState.Closed, TransportState.Error);
            switch (previousState)
            {
                case TransportState.Closed:
                case TransportState.Error:
                    return false;

                case TransportState.NotInitialized:
                case TransportState.Opening:
                    _connectCompletion.TrySetCanceled();
                    break;

                case TransportState.Open:
                case TransportState.Subscribing:
                    _subscribeCompletionSource.TrySetCanceled();
                    break;

                case TransportState.Receiving:
                    _disconnectAwaitersCancellationSource.Cancel();
                    break;

                default:
                    Debug.Fail($"Unknown transport state: {previousState}");
                    throw new InvalidOperationException();
            }
            return true;
        }

        private async Task SubscribeCloudToDeviceMessagesAsync()
        {
            if (TryStateTransition(TransportState.Open, TransportState.Subscribing))
            {
                await _channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(_deviceboundMessageFilter, QualityOfService.AtLeastOnce))).ConfigureAwait(true);

                if (TryStateTransition(TransportState.Subscribing, TransportState.Receiving))
                {
                    if (_subscribeCompletionSource.TryComplete())
                    {
                        return;
                    }
                }
            }
            await _subscribeCompletionSource.Task.ConfigureAwait(true);
        }

        private async Task SubscribeTwinResponsesAsync()
        {
            await _channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(TwinResponseTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_001:  `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_002:  `EnableMethodsAsync` shall wait for a SUBACK for the subscription request.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_003:  `EnableMethodsAsync` shall return failure if the subscription request fails.
            await _channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(MethodPostTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            //SRS_CSHARP_MQTT_TRANSPORT_28_001: `DisableMethodsAsync` shall unsubscribe using the '$iothub/methods/POST/' topic filter.
            //SRS_CSHARP_MQTT_TRANSPORT_28_002: `DisableMethodsAsync` shall wait for a UNSUBACK for the unsubscription.
            //SRS_CSHARP_MQTT_TRANSPORT_28_003: `DisableMethodsAsync` shall return failure if the unsubscription fails.
            await _channel.WriteAsync(new UnsubscribePacket(0, MethodPostTopicFilter)).ConfigureAwait(true);
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_021:  `EnableEventReceiveAsync` shall subscribe using the 'devices/{0}/modules/{1}/' topic filter.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_022:  `EnableEventReceiveAsync` shall wait for a SUBACK for the subscription request.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_023:  `EnableEventReceiveAsync` shall return failure if the subscription request fails.
            await _channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(_receiveEventMessageFilter, _qos))).ConfigureAwait(true);
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            //SRS_CSHARP_MQTT_TRANSPORT_33_021: `DisableEventReceiveAsync` shall unsubscribe using the 'devices/{0}/modules/{1}/#' topic filter.
            //SRS_CSHARP_MQTT_TRANSPORT_33_022: `DisableEventReceiveAsync` shall wait for a UNSUBACK for the unsubscription.
            //SRS_CSHARP_MQTT_TRANSPORT_33_023: `DisableEventReceiveAsync` shall return failure if the unsubscription fails.
            await _channel.WriteAsync(new UnsubscribePacket(0, _receiveEventMessageFilter)).ConfigureAwait(true);
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_005:  `SendMethodResponseAsync` shall allocate a `Message` object containing the method response.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_006:  `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_007:  `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_008:  `SendMethodResponseAsync` shall send the message to the service.
            using var message = new Message(methodResponse.BodyStream)
            {
                MqttTopicName = MethodResponseTopic.FormatInvariant(methodResponse.Status, methodResponse.RequestId)
            };

            await SendEventAsync(message, cancellationToken).ConfigureAwait(true);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_010: `EnableTwinPatchAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_011: `EnableTwinPatchAsync` shall wait for a SUBACK on the subscription request.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_012: `EnableTwinPatchAsync` shall return failure if the subscription request fails.
            await _channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(TwinPatchTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        private bool ParseResponseTopic(string topicName, out string rid, out Int32 status)
        {
            Match match = _twinResponseTopicRegex.Match(topicName);
            if (match.Success)
            {
                status = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);
                rid = HttpUtility.ParseQueryString(match.Groups[2].Value).Get("$rid");
                return true;
            }

            rid = "";
            status = 500;
            return false;
        }

        private async Task<Message> SendTwinRequestAsync(Message request, string rid, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var responseReceived = new SemaphoreSlim(0);
            Message response = null; ;
            ExceptionDispatchInfo responseException = null;

            Action<Message> onTwinResponse = (Message possibleResponse) =>
            {
                try
                {
                    if (ParseResponseTopic(possibleResponse.MqttTopicName, out string receivedRid, out int status))
                    {
                        if (rid == receivedRid)
                        {
                            if (status >= 300)
                            {
                                throw new IotHubException($"Request {rid} returned status {status}", isTransient: false);
                            }
                            else
                            {
                                response = possibleResponse;
                                responseReceived.Release();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    responseException = ExceptionDispatchInfo.Capture(e);
                    responseReceived.Release();
                }
            };

            try
            {
                _twinResponseEvent += onTwinResponse;

                await SendEventAsync(request, cancellationToken).ConfigureAwait(true);

                await responseReceived.WaitAsync(TwinTimeout, cancellationToken).ConfigureAwait(true);

                if (responseException != null)
                {
                    responseException.Throw();
                }
                else if (response == null)
                {
                    throw new TimeoutException($"Response for message {rid} not received");
                }

                return response;
            }
            finally
            {
                _twinResponseEvent -= onTwinResponse;
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_014:  `SendTwinGetAsync` shall allocate a `Message` object to hold the `GET` request
            using var request = new Message();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_015:  `SendTwinGetAsync` shall generate a GUID to use as the $rid property on the request
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_016:  `SendTwinGetAsync` shall set the `Message` topic to '$iothub/twin/GET/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated
            string rid = Guid.NewGuid().ToString();
            request.MqttTopicName = "$iothub/twin/GET/?$rid=" + rid;

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_017:  `SendTwinGetAsync` shall wait for a response from the service with a matching $rid value
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_019:  If the response is failed, `SendTwinGetAsync` shall return that failure to the caller.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_020:  If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error
            using Message response = await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(true);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_021:  If the response contains a success code, `SendTwinGetAsync` shall return success to the caller
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_018:  When a response is received, `SendTwinGetAsync` shall return the Twin object to the caller
            using var reader = new StreamReader(response.GetBodyStream(), System.Text.Encoding.UTF8);
            string body = reader.ReadToEnd();

            try
            {
                return new Twin
                {
                    Properties = JsonConvert.DeserializeObject<TwinProperties>(body),
                };
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled) Logging.Error(this, $"Failed to parse Twin JSON: {ex}. Message body: '{body}'");
                throw;
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_025:  `SendTwinPatchAsync` shall serialize the `reported` object into a JSON string
            var body = JsonConvert.SerializeObject(reportedProperties);
            var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_022:  `SendTwinPatchAsync` shall allocate a `Message` object to hold the update request
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_026:  `SendTwinPatchAsync` shall set the body of the message to the JSON string
            using var request = new Message(bodyStream);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_023:  `SendTwinPatchAsync` shall generate a GUID to use as the $rid property on the request
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_024:  `SendTwinPatchAsync` shall set the `Message` topic to '$iothub/twin/PATCH/properties/reported/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated
            var rid = Guid.NewGuid().ToString();
            request.MqttTopicName = TwinPatchTopic.FormatInvariant(rid);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_027:  `SendTwinPatchAsync` shall wait for a response from the service with a matching $rid value
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_028:  If the response is failed, `SendTwinPatchAsync` shall return that failure to the caller.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_029:  If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinPatchAsync` shall fail with a timeout error.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_030:  If the response contains a success code, `SendTwinPatchAsync` shall return success to the caller.
            await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(true);
        }

        private Func<IPAddress[], int, Task<IChannel>> CreateChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo, ClientOptions options)
        {
            return async (addresses, port) =>
            {
                IChannel channel = null;

                Func<Stream, SslStream> streamFactory = stream => new SslStream(stream, true, settings.RemoteCertificateValidationCallback);

                var certs = settings.ClientCertificate == null
                    ? new List<X509Certificate>(0)
                    : new List<X509Certificate> { settings.ClientCertificate };

                SslProtocols protocols = TlsVersions.Instance.Preferred;
#if NET451
                // Requires hardcoding in NET451 otherwise yields error:
                //    Microsoft.Azure.Devices.Client.Exceptions.IotHubCommunicationException: Transient network error occurred, please retry.
                //    DotNetty.Transport.Channels.ClosedChannelException: I/O error occurred.
                if (settings.GetTransportType() == TransportType.Mqtt_Tcp_Only
                    && protocols == SslProtocols.None)
                {
                    protocols = TlsVersions.Instance.MinimumTlsVersions;
                }
#endif

                var clientTlsSettings = new ClientTlsSettings(
                     protocols,
                     settings.CertificateRevocationCheck,
                     certs,
                     iotHubConnectionString.HostName);

                Bootstrap bootstrap = new Bootstrap()
                    .Group(s_eventLoopGroup.Value)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(ch =>
                    {
                        var tlsHandler = new TlsHandler(streamFactory, clientTlsSettings);

                        ch.Pipeline
                            .AddLast(
                                tlsHandler,
                                MqttEncoder.Instance,
                                new MqttDecoder(false, MaxMessageSize),
                                new LoggingHandler(LogLevel.DEBUG),
                                _mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo, options));
                    }));

                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"Connecting to {address.ToString()}", nameof(CreateChannelFactory));
                        channel = await bootstrap.ConnectAsync(address, port).ConfigureAwait(true);
                        break;
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle((ex) =>
                        {
                            if (ex is ConnectException)     // We will handle DotNetty.Transport.Channels.ConnectException
                            {
                                if (Logging.IsEnabled) Logging.Error(this, $"ConnectException trying to connect to {address.ToString()}: {ex.ToString()}", nameof(CreateChannelFactory));
                                return true;
                            }

                            return false; // Let anything else stop the application.
                        });
                    }
                    catch (ConnectException ex)
                    {
                        //same as above, we will handle DotNetty.Transport.Channels.ConnectException
                        if (Logging.IsEnabled) Logging.Error(this, $"ConnectException trying to connect to {address.ToString()}: {ex.ToString()}", nameof(CreateChannelFactory));
                    }
                }

                if (channel == null)
                {
                    throw new IotHubCommunicationException("MQTT channel open failed.");
                }
                return channel;
            };
        }

        private Func<IPAddress[], int, Task<IChannel>> CreateWebSocketChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo, ClientOptions options)
        {
            return async (address, port) =>
            {
                string additionalQueryParams = "";

                var websocketUri = new Uri(WebSocketConstants.Scheme + iotHubConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                var websocket = new ClientWebSocket();
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);

                try
                {
                    if (IsProxyConfigured())
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = _webProxy;
                        if (Logging.IsEnabled)
                        {
                            Logging.Info(this, $"{nameof(CreateWebSocketChannelFactory)} Setting ClientWebSocket.Options.Proxy");
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    if (Logging.IsEnabled)
                    {
                        Logging.Error(this, $"{nameof(CreateWebSocketChannelFactory)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                    }
                }

                if (settings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(settings.ClientCertificate);
                }

                // Support for RemoteCertificateValidationCallback for ClientWebSocket is introduced in .NET Standard 2.1
#if NETSTANDARD2_1
                if (settings.RemoteCertificateValidationCallback != null)
                {
                    websocket.Options.RemoteCertificateValidationCallback = settings.RemoteCertificateValidationCallback;
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"{nameof(CreateWebSocketChannelFactory)} Setting RemoteCertificateValidationCallback");
                    }
                }
#endif

                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(true);
                }

                var clientChannel = new ClientWebSocketChannel(null, websocket);
                clientChannel
                    .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                    .Option(ChannelOption.AutoRead, false)
                    .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                    .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default)
                    .Pipeline.AddLast(
                        MqttEncoder.Instance,
                        new MqttDecoder(false, MaxMessageSize),
                        new LoggingHandler(LogLevel.DEBUG),
                        _mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo, options));

                await s_eventLoopGroup.Value.RegisterAsync(clientChannel).ConfigureAwait(false);

                return clientChannel;
            };
        }

        private void ScheduleCleanup(Func<Task> cleanupTask)
        {
            Func<Task> currentCleanupFunc = _cleanupFunc;
            _cleanupFunc = async () =>
            {
                await cleanupTask().ConfigureAwait(true);

                if (currentCleanupFunc != null)
                {
                    await currentCleanupFunc().ConfigureAwait(true);
                }
            };
        }

        private async void Cleanup()
        {
            try
            {
                await _closeRetryPolicy.ExecuteAsync(CleanupAsync).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
            }
        }

        private Task CleanupAsync()
        {
            if (_cleanupFunc != null)
            {
                return _cleanupFunc();
            }

            return TaskHelpers.CompletedTask;
        }

        private bool TryStateTransition(TransportState fromState, TransportState toState)
        {
            return (TransportState)Interlocked.CompareExchange(ref _state, (int)toState, (int)fromState) == fromState;
        }

        private void EnsureValidState(bool throwIfNotOpen = true)
        {
            if (State == TransportState.Error)
            {
                _fatalException.Throw();
            }

            if (State == TransportState.Closed)
            {
                Debug.Fail($"{nameof(MqttTransportHandler)}.{nameof(EnsureValidState)}: Attempting to reuse transport after it was closed.");
                throw new InvalidOperationException($"Invalid transport state: {State}");
            }

            if (throwIfNotOpen && (State & TransportState.Open) == 0)
            {
                throw new IotHubCommunicationException("MQTT connection is not established. Please retry later.");
            }
        }

        private static IEventLoopGroup GetEventLoopGroup()
        {
            try
            {
                string envValue = Environment.GetEnvironmentVariable(ProcessorThreadCountVariableName);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    string processorEventCountValue = Environment.ExpandEnvironmentVariables(envValue);
                    if (int.TryParse(processorEventCountValue, out var processorThreadCount))
                    {
                        if (Logging.IsEnabled) Logging.Info(null, $"EventLoopGroup threads count {processorThreadCount}.");
                        return processorThreadCount <= 0 ? new MultithreadEventLoopGroup() :
                            processorThreadCount == 1 ? (IEventLoopGroup)new SingleThreadEventLoop() :
                            new MultithreadEventLoopGroup(processorThreadCount);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Info(null, $"Could not read EventLoopGroup threads count {ex}");
                return new MultithreadEventLoopGroup();
            }

            if (Logging.IsEnabled) Logging.Info(null, "EventLoopGroup threads count was not set.");
            return new MultithreadEventLoopGroup();
        }

        private bool IsProxyConfigured()
        {
            return _webProxy != null && _webProxy != DefaultWebProxySettings.Instance;
        }
    }
}
