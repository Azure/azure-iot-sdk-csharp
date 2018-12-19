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
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    using TransportType = Microsoft.Azure.Devices.Client.TransportType;

    sealed class MqttTransportHandler : TransportHandler, IMqttIotHubEventHandler
    {
        const int ProtocolGatewayPort = 8883;
        const int MaxMessageSize = 256 * 1024;
        const string ProcessorThreadCountVariableName = "MqttEventsProcessorThreadCount";

        static readonly int GenerationPrefixLength = Guid.NewGuid().ToString().Length;

        readonly string generationId = Guid.NewGuid().ToString();

        private static readonly Lazy<IEventLoopGroup> s_eventLoopGroup = new Lazy<IEventLoopGroup>(GetEventLoopGroup);

        readonly string hostName;
        readonly Func<IPAddress[], int, Task<IChannel>> channelFactory;
        readonly Queue<string> completionQueue;
        readonly MqttIotHubAdapterFactory mqttIotHubAdapterFactory;
        readonly QualityOfService qos;

        readonly object syncRoot = new object();
        readonly CancellationTokenSource disconnectAwaitersCancellationSource = new CancellationTokenSource();
        readonly RetryPolicy closeRetryPolicy;

        readonly SemaphoreSlim receivingSemaphore = new SemaphoreSlim(0);
        readonly ConcurrentQueue<Message> messageQueue;

        readonly TaskCompletionSource connectCompletion = new TaskCompletionSource();
        readonly TaskCompletionSource subscribeCompletionSource = new TaskCompletionSource();
        Func<Task> cleanupFunc;
        IChannel channel;
        ExceptionDispatchInfo fatalException;
        IPAddress[] serverAddresses;

        int state = (int)TransportState.NotInitialized;
        public TransportState State => (TransportState)Volatile.Read(ref this.state);

        // incoming topic names
        const string methodPostTopicFilter = "$iothub/methods/POST/#";
        const string methodPostTopicPrefix = "$iothub/methods/POST/";
        const string twinResponseTopicFilter = "$iothub/twin/res/#";
        const string twinResponseTopicPrefix = "$iothub/twin/res/";
        const string twinPatchTopicFilter = "$iothub/twin/PATCH/properties/desired/#";
        const string twinPatchTopicPrefix = "$iothub/twin/PATCH/properties/desired/";
        const string receiveEventMessagePatternFilter = "devices/{0}/modules/{1}/#";
        const string receiveEventMessagePrefixPattern = "devices/{0}/modules/{1}/";

        // outgoing topic names
        const string methodResponseTopic = "$iothub/methods/res/{0}/?$rid={1}";
        const string twinGetTopic = "$iothub/twin/GET/?$rid={0}";
        const string twinPatchTopic = "$iothub/twin/PATCH/properties/reported/?$rid={0}";

        // incoming topic regexp
        const string twinResponseTopicPattern = @"\$iothub/twin/res/(\d+)/(\?.+)";
        private static readonly TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        Regex twinResponseTopicRegex = new Regex(twinResponseTopicPattern, RegexOptions.Compiled, regexTimeoutMilliseconds);

        Func<MethodRequestInternal, Task> messageListener;
        Action<TwinCollection> onDesiredStatePatchListener;
        Action<Message> twinResponseEvent;
        Func<string, Message, Task> messageReceivedListener;

        string receiveEventMessageFilter;
        string receiveEventMessagePrefix;

        public TimeSpan TwinTimeout = TimeSpan.FromSeconds(60);

        internal MqttTransportHandler(
            IPipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onReceiveCallback = null)
            : this(context, iotHubConnectionString, settings, null)
        {
            this.messageListener = onMethodCallback;
            this.messageReceivedListener = onReceiveCallback;
            this.onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;
        }

        internal MqttTransportHandler(
            IPipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<IPAddress[], int, Task<IChannel>> channelFactory)
            : base(context, settings)
        {
            this.mqttIotHubAdapterFactory = new MqttIotHubAdapterFactory(settings);
            this.messageQueue = new ConcurrentQueue<Message>();
            this.completionQueue = new Queue<string>();

            this.serverAddresses = null; // this will be resolved asynchronously in OpenAsync
            this.hostName = iotHubConnectionString.HostName;
            this.receiveEventMessageFilter = string.Format(CultureInfo.InvariantCulture, receiveEventMessagePatternFilter, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);
            this.receiveEventMessagePrefix = string.Format(CultureInfo.InvariantCulture, receiveEventMessagePrefixPattern, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);

            this.qos = settings.PublishToServerQoS;

            if (channelFactory == null)
            {
                switch (settings.GetTransportType())
                {
                    case TransportType.Mqtt_Tcp_Only:
                        this.channelFactory = this.CreateChannelFactory(iotHubConnectionString, settings, context.Get<ProductInfo>());
                        break;
                    case TransportType.Mqtt_WebSocket_Only:
                        this.channelFactory = this.CreateWebSocketChannelFactory(iotHubConnectionString, settings, context.Get<ProductInfo>());
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Transport Setting {0}".FormatInvariant(settings.GetTransportType()));
                }
            }
            else
            {
                this.channelFactory = channelFactory;
            }

            this.closeRetryPolicy = new RetryPolicy(new TransientErrorIgnoreStrategy(), 5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

#region Client operations

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                cancellationToken.ThrowIfCancellationRequested();

                this.EnsureValidState();

                if (State.HasFlag(TransportState.Open))
                {
                    return;
                }

                await this.OpenAsyncInternal(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(OpenAsync));
            }
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(SendEventAsync));
                cancellationToken.ThrowIfCancellationRequested();

                this.EnsureValidState();
                Debug.Assert(channel != null);

                return this.channel.WriteAndFlushAsync(message);
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
                await this.SendEventAsync(message, cancellationToken).ConfigureAwait(true);
            }
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Message message = null;

            cancellationToken.ThrowIfCancellationRequested();

            this.EnsureValidState();

            if (this.State != TransportState.Receiving)
            {
                await this.SubscribeAsync().ConfigureAwait(true);
            }

            bool hasMessage = await this.ReceiveMessageArrivalAsync(timeout, cancellationToken).ConfigureAwait(true);

            if (hasMessage)
            {
                lock (this.syncRoot)
                {
                    this.messageQueue.TryDequeue(out message);
                    message.LockToken = message.LockToken;
                    if (this.qos == QualityOfService.AtLeastOnce)
                    {
                        this.completionQueue.Enqueue(message.LockToken);
                    }

                    message.LockToken = this.generationId + message.LockToken;
                }
            }

            return message;
        }

        async Task<bool> ReceiveMessageArrivalAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            bool hasMessage = false;
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disconnectAwaitersCancellationSource.Token))
            {
                hasMessage = await this.receivingSemaphore.WaitAsync(timeout, linkedCts.Token).ConfigureAwait(true);
            }

            return hasMessage;
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            if (this.qos == QualityOfService.AtMostOnce)
            {
                throw new IotHubException("Complete is not allowed for QoS 0.", isTransient: false);
            }

            Task completeOperationCompletion;
            lock (this.syncRoot)
            {
                if (!lockToken.StartsWith(this.generationId))
                {
                    throw new IotHubException(
                        "Lock token is stale or never existed. The message will be redelivered, please discard this lock token and do not retry operation.",
                        isTransient:false);
                }

                if (this.completionQueue.Count == 0)
                {
                    throw new IotHubException("Unknown lock token.", isTransient:false);
                }

                string actualLockToken = this.completionQueue.Peek();
                if (lockToken.IndexOf(actualLockToken, GenerationPrefixLength, StringComparison.Ordinal) != GenerationPrefixLength ||
                    lockToken.Length != actualLockToken.Length + GenerationPrefixLength)
                {
                    throw new IotHubException(
                        $"Client MUST send PUBACK packets in the order in which the corresponding PUBLISH packets were received (QoS 1 messages) per [MQTT-4.6.0-2]. Expected lock token: '{actualLockToken}'; actual lock token: '{lockToken}'.",
                        isTransient:false);
                }

                this.completionQueue.Dequeue();
                completeOperationCompletion = this.channel.WriteAndFlushAsync(actualLockToken);
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
            try
            {
                if (disposing)
                {
                    if (this.TryStop())
                    {
                        this.Cleanup();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, "", $"{nameof(MqttTransportHandler)}.{nameof(CloseAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                if (this.TryStop())
                {
                    _transportShouldRetry.TrySetCanceled();

                    await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync, cancellationToken).ConfigureAwait(true);
                }
                else
                {
                    if (this.State == TransportState.Error)
                    {
                        this.fatalException.Throw();
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, "", $"{nameof(MqttTransportHandler)}.{nameof(CloseAsync)}");
            }
        }

        #endregion

        #region MQTT callbacks
        public void OnConnected()
        {
            if (this.TryStateTransition(TransportState.Opening, TransportState.Open))
            {
                this.connectCompletion.TryComplete();
            }
        }

        async Task HandleIncomingTwinPatch(Message message)
        {
            try
            {
                if (this.onDesiredStatePatchListener != null)
                {
                    using (StreamReader reader = new StreamReader(message.GetBodyStream(), System.Text.Encoding.UTF8))
                    {
                        string patch = reader.ReadToEnd();
                        var props = JsonConvert.DeserializeObject<TwinCollection>(patch);
                        await Task.Run(() => this.onDesiredStatePatchListener(props)).ConfigureAwait(true);
                    }
                }
            }
            finally
            {
                message.Dispose();
            }
        }

        async Task HandleIncomingMethodPost(Message message)
        {
            try
            {
                string[] tokens = Regex.Split(message.MqttTopicName, "/", RegexOptions.Compiled, regexTimeoutMilliseconds);

                var mr = new MethodRequestInternal(tokens[3], tokens[4].Substring(6), message.BodyStream, CancellationToken.None);
                await Task.Run(() =>this.messageListener(mr)).ConfigureAwait(true);
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
                if ((this.State & TransportState.Open) == TransportState.Open)
                {
                    if (message.MqttTopicName.StartsWith(twinResponseTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        twinResponseEvent(message);
                    }
                    else if (message.MqttTopicName.StartsWith(twinPatchTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingTwinPatch(message).ConfigureAwait(true);
                    }
                    else if (message.MqttTopicName.StartsWith(methodPostTopicPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingMethodPost(message).ConfigureAwait(true);
                    }
                    else if (message.MqttTopicName.StartsWith(this.receiveEventMessagePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleIncomingEventMessage(message).ConfigureAwait(true);
                    }
                    else
                    {
                        this.messageQueue.Enqueue(message);
                        this.receivingSemaphore.Release();
                    }
                }
            }
            catch(Exception ex)
            {
                this.OnError(ex);
            }
        }

        async Task HandleIncomingEventMessage(Message message)
        {
            try
            {
                // The MqttTopic is in the format - devices/deviceId/modules/moduleId/inputs/inputName
                // We try to get the endpoint from the topic, if the topic is in the above format.
                string[] tokens = message.MqttTopicName.Split('/');
                string inputName = tokens.Length >= 6 ? tokens[5] : null;

                // Add the endpoint as a SystemProperty
                message.SystemProperties.Add(MessageSystemPropertyNames.InputName, inputName);

                if (this.qos == QualityOfService.AtLeastOnce)
                {
                    lock (this.syncRoot)
                    {
                        this.completionQueue.Enqueue(message.LockToken);
                    }
                }
                message.LockToken = this.generationId + message.LockToken;
                await (this.messageReceivedListener?.Invoke(inputName, message) ?? TaskHelpers.CompletedTask).ConfigureAwait(true);
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
                TransportState previousState = this.MoveToStateIfPossible(TransportState.Error, TransportState.Closed);
                switch (previousState)
                {
                    case TransportState.Error:
                    case TransportState.Closed:
                        return;
                    case TransportState.NotInitialized:
                    case TransportState.Opening:
                        this.fatalException = ExceptionDispatchInfo.Capture(exception);
                        this.connectCompletion.TrySetException(exception);
                        this.subscribeCompletionSource.TrySetException(exception);
                        break;
                    case TransportState.Open:
                    case TransportState.Subscribing:
                        this.fatalException = ExceptionDispatchInfo.Capture(exception);
                        this.subscribeCompletionSource.TrySetException(exception);
                        _transportShouldRetry.TrySetResult(true);
                        break;
                    case TransportState.Receiving:
                        this.fatalException = ExceptionDispatchInfo.Capture(exception);
                        this.disconnectAwaitersCancellationSource.Cancel();
                        _transportShouldRetry.TrySetResult(true);
                        break;
                    default:
                        Debug.Fail($"Unknown transport state: {previousState}");
                        throw new InvalidOperationException();
                }

                await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (Logging.IsEnabled) Logging.Error(this, ex.ToString(), nameof(OnError));
            }
        }
        
        TransportState MoveToStateIfPossible(TransportState destination, TransportState illegalStates)
        {
            TransportState previousState = this.State;
            do
            {
                if ((previousState & illegalStates) > 0)
                {
                    return previousState;
                }
                TransportState prevState;
                if ((prevState = (TransportState)Interlocked.CompareExchange(ref this.state, (int)destination, (int)previousState)) == previousState)
                {
                    return prevState;
                }
                previousState = prevState;
            }
            while (true);
        }

#endregion

        async Task OpenAsyncInternal(CancellationToken cancellationToken)
        {
#if NET451
            this.serverAddresses = Dns.GetHostEntry(this.hostName).AddressList;
#else
            this.serverAddresses = (await Dns.GetHostAddressesAsync(this.hostName).ConfigureAwait(true));
#endif
            if (this.TryStateTransition(TransportState.NotInitialized, TransportState.Opening))
            {
                try
                {
                    this.channel = await this.channelFactory(this.serverAddresses, ProtocolGatewayPort).ConfigureAwait(true);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    this.OnError(ex);
                    throw;
                }

                this.ScheduleCleanup(async () =>
                {
                    this.disconnectAwaitersCancellationSource.Cancel();
                    if (this.channel == null)
                    {
                        return;
                    }
                    if (this.channel.Active)
                    {
                        await this.channel.WriteAsync(DisconnectPacket.Instance).ConfigureAwait(true);
                    }
                    if (this.channel.Open)
                    {
                        await this.channel.CloseAsync().ConfigureAwait(true);
                    }
                });
            }

            await this.connectCompletion.Task.ConfigureAwait(true);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_031: `OpenAsync` shall subscribe using the '$iothub/twin/res/#' topic filter
            await this.SubscribeTwinResponsesAsync().ConfigureAwait(true);
        }

        bool TryStop()
        {
            TransportState previousState = this.MoveToStateIfPossible(TransportState.Closed, TransportState.Error);
            switch (previousState)
            {
                case TransportState.Closed:
                case TransportState.Error:
                    return false;
                case TransportState.NotInitialized:
                case TransportState.Opening:
                    this.connectCompletion.TrySetCanceled();
                    break;
                case TransportState.Open:
                case TransportState.Subscribing:
                    this.subscribeCompletionSource.TrySetCanceled();
                    break;
                case TransportState.Receiving:
                    this.disconnectAwaitersCancellationSource.Cancel();
                    break;
                default:
                    Debug.Fail($"Unknown transport state: {previousState}");
                    throw new InvalidOperationException();
            }
            return true;
        }

        async Task SubscribeAsync()
        {
            if (this.TryStateTransition(TransportState.Open, TransportState.Subscribing))
            {
                await this.channel.WriteAsync(new SubscribePacket()).ConfigureAwait(true);
                
                if (this.TryStateTransition(TransportState.Subscribing, TransportState.Receiving))
                {
                    if (this.subscribeCompletionSource.TryComplete())
                    {
                        return;
                    }
                }
            }
            await this.subscribeCompletionSource.Task.ConfigureAwait(true);
        }

        async Task SubscribeTwinResponsesAsync()
        {
            await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(twinResponseTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_001:  `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_002:  `EnableMethodsAsync` shall wait for a SUBACK for the subscription request. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_003:  `EnableMethodsAsync` shall return failure if the subscription request fails. 
            await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(methodPostTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            //SRS_CSHARP_MQTT_TRANSPORT_28_001: `DisableMethodsAsync` shall unsubscribe using the '$iothub/methods/POST/' topic filter.
            //SRS_CSHARP_MQTT_TRANSPORT_28_002: `DisableMethodsAsync` shall wait for a UNSUBACK for the unsubscription.
            //SRS_CSHARP_MQTT_TRANSPORT_28_003: `DisableMethodsAsync` shall return failure if the unsubscription fails.
            await this.channel.WriteAsync(new UnsubscribePacket(0, methodPostTopicFilter)).ConfigureAwait(true);
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_021:  `EnableEventReceiveAsync` shall subscribe using the 'devices/{0}/modules/{1}/' topic filter. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_022:  `EnableEventReceiveAsync` shall wait for a SUBACK for the subscription request. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_023:  `EnableEventReceiveAsync` shall return failure if the subscription request fails. 
            await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(this.receiveEventMessageFilter, this.qos))).ConfigureAwait(true);
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            //SRS_CSHARP_MQTT_TRANSPORT_33_021: `DisableEventReceiveAsync` shall unsubscribe using the 'devices/{0}/modules/{1}/#' topic filter.
            //SRS_CSHARP_MQTT_TRANSPORT_33_022: `DisableEventReceiveAsync` shall wait for a UNSUBACK for the unsubscription.
            //SRS_CSHARP_MQTT_TRANSPORT_33_023: `DisableEventReceiveAsync` shall return failure if the unsubscription fails.
            await this.channel.WriteAsync(new UnsubscribePacket(0, this.receiveEventMessageFilter)).ConfigureAwait(true);
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_005:  `SendMethodResponseAsync` shall allocate a `Message` object containing the method response. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_006:  `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_007:  `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object. 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_008:  `SendMethodResponseAsync` shall send the message to the service. 
            var message = new Message(methodResponse.BodyStream);

            message.MqttTopicName = methodResponseTopic.FormatInvariant(methodResponse.Status, methodResponse.RequestId);

            await this.SendEventAsync(message, cancellationToken).ConfigureAwait(true);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_010: `EnableTwinPatchAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_011: `EnableTwinPatchAsync` shall wait for a SUBACK on the subscription request.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_012: `EnableTwinPatchAsync` shall return failure if the subscription request fails.
            await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(twinPatchTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(true);
        }

        Boolean ParseResponseTopic(string topicName, out string rid, out Int32 status)
        {
            var match = this.twinResponseTopicRegex.Match(topicName);
            if (match.Success)
            {
                status = Convert.ToInt32(match.Groups[1].Value);
                rid = HttpUtility.ParseQueryString(match.Groups[2].Value).Get("$rid");
                return true;
            }
            else
            {
                rid = "";
                status = 500;
                return false;
            }
        }

        async Task<Message> SendTwinRequestAsync(Message request, string rid, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var responseReceived = new SemaphoreSlim(0);
            Message response = null; ;
            ExceptionDispatchInfo responseException = null;

            Action<Message> onTwinResponse = (Message possibleResponse) =>
            {
                try
                {
                    string receivedRid;
                    int status;

                    if (ParseResponseTopic(possibleResponse.MqttTopicName, out receivedRid, out status))
                    {
                        if (rid == receivedRid)
                        {
                            if (status >= 300)
                            {
                                throw new IotHubException("request " + rid + " returned status " + status.ToString(), isTransient:false);
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
                this.twinResponseEvent += onTwinResponse;

                await this.SendEventAsync(request, cancellationToken).ConfigureAwait(true);

                await responseReceived.WaitAsync(this.TwinTimeout, cancellationToken).ConfigureAwait(true);

                if (responseException != null)
                {
                    responseException.Throw();
                }
                else if (response == null)
                {
                    throw new TimeoutException("Response for message " + rid + " not received");
                }

                return response;
            }
            finally
            {
                twinResponseEvent -= onTwinResponse;
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Twin twin = null;
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_014:  `SendTwinGetAsync` shall allocate a `Message` object to hold the `GET` request 
            var request = new Message();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_015:  `SendTwinGetAsync` shall generate a GUID to use as the $rid property on the request 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_016:  `SendTwinGetAsync` shall set the `Message` topic to '$iothub/twin/GET/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated 
            string rid = Guid.NewGuid().ToString(); ;
            request.MqttTopicName = "$iothub/twin/GET/?$rid=" + rid;

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_017:  `SendTwinGetAsync` shall wait for a response from the service with a matching $rid value 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_019:  If the response is failed, `SendTwinGetAsync` shall return that failure to the caller.
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_020:  If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error 
            using (var response = await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(true))
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_021:  If the response contains a success code, `SendTwinGetAsync` shall return success to the caller  
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_018:  When a response is received, `SendTwinGetAsync` shall return the Twin object to the caller
                using (StreamReader reader = new StreamReader(response.GetBodyStream(), System.Text.Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();

                    try
                    {
                        var props = JsonConvert.DeserializeObject<TwinProperties>(body);

                        twin = new Twin();
                        twin.Properties = props;
                    }
                    catch (JsonReaderException ex)
                    {
                        if (Logging.IsEnabled) Logging.Error(this, $"Failed to parse Twin JSON: {ex}. Message body: '{body}'");
                        throw;
                    }
                }
            }

            return twin;
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.EnsureValidState();

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_025:  `SendTwinPatchAsync` shall serialize the `reported` object into a JSON string 
            var body = JsonConvert.SerializeObject(reportedProperties);
            var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_022:  `SendTwinPatchAsync` shall allocate a `Message` object to hold the update request 
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_026:  `SendTwinPatchAsync` shall set the body of the message to the JSON string 
            using (var request = new Message(bodyStream))
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_023:  `SendTwinPatchAsync` shall generate a GUID to use as the $rid property on the request 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_024:  `SendTwinPatchAsync` shall set the `Message` topic to '$iothub/twin/PATCH/properties/reported/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated 
                var rid = Guid.NewGuid().ToString();
                request.MqttTopicName = twinPatchTopic.FormatInvariant(rid);

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_027:  `SendTwinPatchAsync` shall wait for a response from the service with a matching $rid value 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_028:  If the response is failed, `SendTwinPatchAsync` shall return that failure to the caller. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_029:  If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinPatchAsync` shall fail with a timeout error.  
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_030:  If the response contains a success code, `SendTwinPatchAsync` shall return success to the caller. 
                await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(true);
            }
        }

        Func<IPAddress[], int, Task<IChannel>> CreateChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo)
        {
            return async (addresses, port) =>
            {
                IChannel channel = null;

                Func<Stream, SslStream> streamFactory = stream => new SslStream(stream, true, settings.RemoteCertificateValidationCallback);
                var clientTlsSettings = settings.ClientCertificate != null ?
                    new ClientTlsSettings(iotHubConnectionString.HostName, new List<X509Certificate> { settings.ClientCertificate }) :
                    new ClientTlsSettings(iotHubConnectionString.HostName);
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
                                this.mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo));
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
                }

                return channel;
            };
        }

        Func<IPAddress[], int, Task<IChannel>> CreateWebSocketChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo)
        {
            return async (address, port) =>
            {
                string additionalQueryParams = "";
#if NETSTANDARD1_3
                // NETSTANDARD1_3 implementation doesn't set client certs, so we want to tell the IoT Hub to not ask for them
                additionalQueryParams = "?iothub-no-client-cert=true";
#endif

                var websocketUri = new Uri(WebSocketConstants.Scheme + iotHubConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                var websocket = new ClientWebSocket();
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = settings.Proxy;

                try
                {
                    if (webProxy != DefaultWebProxySettings.Instance)
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = webProxy;
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
                        this.mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo));

                await s_eventLoopGroup.Value.RegisterAsync(clientChannel).ConfigureAwait(false);

                return clientChannel;
            };
        }

        void ScheduleCleanup(Func<Task> cleanupTask)
        {
            Func<Task> currentCleanupFunc = this.cleanupFunc;
            this.cleanupFunc = async () =>
            {
                await cleanupTask().ConfigureAwait(true);

                if (currentCleanupFunc != null)
                {
                    await currentCleanupFunc().ConfigureAwait(true);
                }
            };
        }

        async void Cleanup()
        {
            try
            {
                await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync).ConfigureAwait(true);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
            }
        }

        Task CleanupAsync()
        {
            if (this.cleanupFunc != null)
            {
                return this.cleanupFunc();
            }

            return TaskHelpers.CompletedTask;
        }

        bool TryStateTransition(TransportState fromState, TransportState toState)
        {
            return (TransportState)Interlocked.CompareExchange(ref this.state, (int)toState, (int)fromState) == fromState;
        }

        void EnsureValidState()
        {
            if (this.State == TransportState.Error)
            {
                this.fatalException.Throw();
            }
            if (this.State == TransportState.Closed)
            {
                Debug.Fail($"{nameof(MqttTransportHandler)}.{nameof(EnsureValidState)}: Attempting to reuse transport after it was closed.");
                throw new InvalidOperationException($"Invalid transport state: {this.State}");
            }
        }

        static IEventLoopGroup GetEventLoopGroup()
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
                            processorThreadCount == 1 ? (IEventLoopGroup) new SingleThreadEventLoop() :
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
    }
}
