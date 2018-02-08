// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
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
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Mqtt;
    using DotNetty.Codecs.Mqtt.Packets;
    using DotNetty.Common.Concurrency;
#if !WINDOWS_UWP
    using DotNetty.Handlers.Tls;
#endif
#if WINDOWS_UWP
    using DotNetty.Codecs;
    using DotNetty.Common.Internal;
    using Windows.Networking;
    using Windows.Networking.Sockets;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Storage.Streams;
    using Windows.System.Diagnostics;
    using Windows.System.Profile;
#endif

    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Client.TransientFaultHandling;
    using Newtonsoft.Json;
    using TransportType = Microsoft.Azure.Devices.Client.TransportType;

#if !WINDOWS_UWP
    using System.Web;
#endif

#if WINDOWS_UWP
    //
    // Implementation of the UWP platform used to provide UWP-specific functionality for DotNetty
    //
    class UWPPlatform : IPlatform
    {
        int IPlatform.GetCurrentProcessId() => (int)ProcessDiagnosticInfo.GetForCurrentProcess().ProcessId;

        byte[] IPlatform.GetDefaultDeviceId()
        {
            var signature = new byte[8];
            int index = 0;
            HardwareToken hardwareToken = HardwareIdentification.GetPackageSpecificToken(null);
            using (DataReader dataReader = DataReader.FromBuffer(hardwareToken.Id))
            {
                int offset = 0;
                while (offset < hardwareToken.Id.Length && index < 7)
                {
                    var hardwareEntry = new byte[4];
                    dataReader.ReadBytes(hardwareEntry);
                    byte componentID = hardwareEntry[0];
                    byte componentIDReserved = hardwareEntry[1];

                    if (componentIDReserved == 0)
                    {
                        switch (componentID)
                        {
                            // Per guidance in http://msdn.microsoft.com/en-us/library/windows/apps/jj553431
                            case 1: // CPU
                            case 2: // Memory
                            case 4: // Network Adapter
                            case 9: // Bios
                                signature[index++] = hardwareEntry[2];
                                signature[index++] = hardwareEntry[3];
                                break;
                            default:
                                break;
                        }
                    }
                    offset += 4;
                }
            }
            return signature;
        }
    }
#endif

    sealed class MqttTransportHandler : TransportHandler, IMqttIotHubEventHandler
    {
        const int ProtocolGatewayPort = 8883;
        const int MaxMessageSize = 256 * 1024;

        static readonly int GenerationPrefixLength = Guid.NewGuid().ToString().Length;

        readonly string generationId = Guid.NewGuid().ToString();

        static readonly ConcurrentObjectPool<string, IEventLoopGroup> EventLoopGroupPool =
            new ConcurrentObjectPool<string, IEventLoopGroup>(
                Environment.ProcessorCount,
                () => new MultithreadEventLoopGroup(eg => new SingleThreadEventLoop(eg, "MQTTExecutionThread", TimeSpan.FromSeconds(1)), 1),
                TimeSpan.FromSeconds(5),
                elg => elg.ShutdownGracefullyAsync());

        readonly string hostName;
        readonly Func<IPAddress, int, Task<IChannel>> channelFactory;
        readonly Queue<string> completionQueue;
        readonly MqttIotHubAdapterFactory mqttIotHubAdapterFactory;
        readonly QualityOfService qos;

        readonly string eventLoopGroupKey;
        readonly object syncRoot = new object();
        readonly CancellationTokenSource disconnectAwaitersCancellationSource = new CancellationTokenSource();
        readonly RetryPolicy closeRetryPolicy;

        readonly SemaphoreSlim receivingSemaphore = new SemaphoreSlim(0);
        readonly ConcurrentQueue<Message> messageQueue;

        readonly TaskCompletionSource connectCompletion = new TaskCompletionSource();
        readonly TaskCompletionSource subscribeCompletionSource = new TaskCompletionSource();
        Func<Task> cleanupFunc;
        IChannel channel;
        Exception fatalException;
        IPAddress serverAddress;

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
        Regex twinResponseTopicRegex = new Regex(twinResponseTopicPattern, RegexOptions.None);

        Action<object, ConnectionEventArgs> connectionOpenedListener;
        Func<object, ConnectionEventArgs, Task> connectionClosedListener;
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
            Action<object, ConnectionEventArgs> onConnectionOpenedCallback,
            Func<object, ConnectionEventArgs, Task> onConnectionClosedCallback,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onReceiveCallback = null)
            : this(context, iotHubConnectionString, settings, null, onConnectionOpenedCallback, onConnectionClosedCallback)
        {
            this.messageListener = onMethodCallback;
            this.messageReceivedListener = onReceiveCallback;
            this.onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;
        }

        internal MqttTransportHandler(
            IPipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings settings,
            Func<IPAddress, int, Task<IChannel>> channelFactory,
            Action<object, ConnectionEventArgs> onConnectionOpenedCallback,
            Func<object, ConnectionEventArgs, Task> onConnectionClosedCallback)
            : base(context, settings)
        {
            this.connectionOpenedListener = onConnectionOpenedCallback;
            this.connectionClosedListener = onConnectionClosedCallback;

            this.mqttIotHubAdapterFactory = new MqttIotHubAdapterFactory(settings);
            this.messageQueue = new ConcurrentQueue<Message>();
            this.completionQueue = new Queue<string>();

            this.serverAddress = null; // this will be resolved asynchronously in OpenAsync
            this.hostName = iotHubConnectionString.HostName;
            this.receiveEventMessageFilter = string.Format(CultureInfo.InvariantCulture, receiveEventMessagePatternFilter, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);
            this.receiveEventMessagePrefix = string.Format(CultureInfo.InvariantCulture, receiveEventMessagePrefixPattern, iotHubConnectionString.DeviceId, iotHubConnectionString.ModuleId);

            this.qos = settings.PublishToServerQoS;
            this.eventLoopGroupKey = iotHubConnectionString.IotHubName + "#" + iotHubConnectionString.DeviceId + "#" + iotHubConnectionString.Audience;

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

        bool TransportIsOpen()
        {
            return this.State.HasFlag(TransportState.Open);
        }
        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            if (this.TransportIsOpen())
            {
                return;
            }

            Debug.WriteLine(cancellationToken.GetHashCode() + " MqttTransportHandler.OpenAsync()");
            await this.HandleTimeoutCancellation(this.OpenAsync, cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(() =>
            {
                if (this.channel == null && cancellationToken.IsCancellationRequested)
                {
                    return TaskConstants.Completed;
                }

                return this.channel.WriteAndFlushAsync(message);
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
                foreach (Message message in messages)
                {
                    await this.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Message message = null;

            await this.HandleTimeoutCancellation(async () =>
            {
                this.EnsureValidState();

                if (this.State != TransportState.Receiving)
                {
                    await this.SubscribeAsync().ConfigureAwait(false);
                }

                bool hasMessage = await this.ReceiveMessageArrivalAsync(timeout, cancellationToken).ConfigureAwait(false);

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
            }, cancellationToken).ConfigureAwait(false);

            return message;
        }

        async Task<bool> ReceiveMessageArrivalAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            bool hasMessage = false;
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disconnectAwaitersCancellationSource.Token))
            {
                hasMessage = await this.receivingSemaphore.WaitAsync(timeout, linkedCts.Token).ConfigureAwait(false);
            }
            return hasMessage;
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            if (this.qos == QualityOfService.AtMostOnce)
            {
                throw new IotHubClientTransientException("Complete is not allowed for QoS 0.");
            }

            await this.HandleTimeoutCancellation(async () =>
            {
                Task completeOperationCompletion;
                lock (this.syncRoot)
                {
                    if (!lockToken.StartsWith(this.generationId))
                    {
                        throw new IotHubClientTransientException("Lock token is stale or never existed. The message will be redelivered, please discard this lock token and do not retry operation.");
                    }

                    if (this.completionQueue.Count == 0)
                    {
                        throw new IotHubClientTransientException("Unknown lock token.");
                    }

                    string actualLockToken = this.completionQueue.Peek();
                    if (lockToken.IndexOf(actualLockToken, GenerationPrefixLength, StringComparison.Ordinal) != GenerationPrefixLength ||
                        lockToken.Length != actualLockToken.Length + GenerationPrefixLength)
                    {
                        throw new IotHubException($"Client MUST send PUBACK packets in the order in which the corresponding PUBLISH packets were received (QoS 1 messages) per [MQTT-4.6.0-2]. Expected lock token: '{actualLockToken}'; actual lock token: '{lockToken}'.");
                    }

                    this.completionQueue.Dequeue();
                    completeOperationCompletion = this.channel.WriteAndFlushAsync(actualLockToken);
                }
                await completeOperationCompletion.ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            throw new IotHubException("MQTT protocol does not support this operation");
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            throw new IotHubException("MQTT protocol does not support this operation");
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

        public override async Task CloseAsync()
        {
            Debug.WriteLine("MqttTransportHandler.CloseAsync()");

            if (this.TryStop())
            {
                await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync).ConfigureAwait(false);
                await this.connectionClosedListener(
                    this.channel,
                    new ConnectionEventArgs
                    {
                        ConnectionType = ConnectionType.MqttConnection,
                        ConnectionStatus = ConnectionStatus.Disabled,
                        ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close
                    }).ConfigureAwait(false);
            }
            else
            {
                if (this.State == TransportState.Error)
                {
                    throw new IotHubClientException(this.fatalException);
                }
            }
        }

#endregion

#region MQTT callbacks
        public void OnConnected()
        {
            if (this.TryStateTransition(TransportState.Opening, TransportState.Open))
            {
                this.connectCompletion.TryComplete();
                Task.Run(() => this.connectionOpenedListener(
                    this.channel,
                    new ConnectionEventArgs
                    {
                        ConnectionType = ConnectionType.MqttConnection,
                        ConnectionStatus = ConnectionStatus.Connected,
                        ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok
                    }));
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
                        await Task.Run(() => this.onDesiredStatePatchListener(props)).ConfigureAwait(false);
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
                string[] tokens = System.Text.RegularExpressions.Regex.Split(message.MqttTopicName, "/");

                var mr = new MethodRequestInternal(tokens[3], tokens[4].Substring(6), message.BodyStream);
                await Task.Run(() =>this.messageListener(mr)).ConfigureAwait(false);
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
                    if (message.MqttTopicName.StartsWith(twinResponseTopicPrefix))
                    {
                        twinResponseEvent(message);
                    }
                    else if (message.MqttTopicName.StartsWith(twinPatchTopicPrefix))
                    {
                        await HandleIncomingTwinPatch(message).ConfigureAwait(false);
                    }
                    else if (message.MqttTopicName.StartsWith(methodPostTopicPrefix))
                    {
                        await HandleIncomingMethodPost(message).ConfigureAwait(false);
                    }
                    else if (message.MqttTopicName.StartsWith(this.receiveEventMessagePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await this.HandleIncomingEventMessage(message);
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
                await (this.messageReceivedListener?.Invoke(inputName, message) ?? Task.FromResult(true));
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
                        this.fatalException = exception;
                        this.connectCompletion.TrySetException(exception);
                        this.subscribeCompletionSource.TrySetException(exception);
                        break;
                    case TransportState.Open:
                    case TransportState.Subscribing:
                        this.fatalException = exception;
                        this.subscribeCompletionSource.TrySetException(exception);
                        break;
                    case TransportState.Receiving:
                        this.fatalException = exception;
                        this.disconnectAwaitersCancellationSource.Cancel();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync).ConfigureAwait(false);

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_28_04: If OnError is triggered after OpenAsync is called, onConnectionClosedCallback shall be invoked.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_28_05: If OnError is triggered after ReceiveAsync is called, onConnectionClosedCallback shall be invoked.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_28_06: If OnError is triggered without any prior operation, onConnectionClosedCallback shall not be invoked.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_28_07: If OnError is triggered in error state, onConnectionClosedCallback shall not be invoked.

                if ((previousState & TransportState.Open) == TransportState.Open)
                {
                    await Task.Run(async () => await this.connectionClosedListener(
                        this.channel,
                        new ConnectionEventArgs
                        {
                            ConnectionType = ConnectionType.MqttConnection,
                            ConnectionStatus = ConnectionStatus.Disconnected_Retrying,
                            ConnectionStatusChangeReason = ConnectionStatusChangeReason.No_Network
                        })).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {

            }
        }

        public override Task RecoverConnections(object link, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            // Codes_SRS_CSHARP_MQTT_TRANSPORT_28_08: [** `RecoverConnections` shall throw IotHubClientException exception when in error state.
            this.EnsureValidState();
            return Common.TaskConstants.Completed;
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

        async Task OpenAsync()
        {
            Debug.WriteLine("MqttTransportHandler.OpenAsync()");

#if WINDOWS_UWP
            HostName host = new HostName(this.hostName);
            var endpointPairs = await DatagramSocket.GetEndpointPairsAsync(host, "");
            var ep = endpointPairs.First();
            this.serverAddress = IPAddress.Parse(ep.RemoteHostName.RawName);
#else
#if !NETSTANDARD1_3
            this.serverAddress = Dns.GetHostEntry(this.hostName).AddressList[0];
#else
            this.serverAddress = (await Dns.GetHostAddressesAsync(this.hostName).ConfigureAwait(false))[0];
#endif
#endif
            if (this.TryStateTransition(TransportState.NotInitialized, TransportState.Opening))
            {
                try
                {
                    this.channel = await this.channelFactory(this.serverAddress, ProtocolGatewayPort).ConfigureAwait(false);
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
                        await this.channel.WriteAsync(DisconnectPacket.Instance).ConfigureAwait(false);
                    }
                    if (this.channel.Open)
                    {
                        await this.channel.CloseAsync().ConfigureAwait(false);
                    }
                });
            }

            await this.connectCompletion.Task.ConfigureAwait(false);

            // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_031: `OpenAsync` shall subscribe using the '$iothub/twin/res/#' topic filter
            await this.SubscribeTwinResponsesAsync().ConfigureAwait(false);
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
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        async Task SubscribeAsync()
        {
            if (this.TryStateTransition(TransportState.Open, TransportState.Subscribing))
            {
                await this.channel.WriteAsync(new SubscribePacket()).ConfigureAwait(false);
                
                if (this.TryStateTransition(TransportState.Subscribing, TransportState.Receiving))
                {
                    if (this.subscribeCompletionSource.TryComplete())
                    {
                        return;
                    }
                }
            }
            await this.subscribeCompletionSource.Task.ConfigureAwait(false);
        }

        async Task SubscribeTwinResponsesAsync()
        {
            await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(twinResponseTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(false);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_032: `EnableMethodsAsync` shall open the transport if this method is called when the transport is not open.
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync().ConfigureAwait(false);
                }

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_001:  `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_002:  `EnableMethodsAsync` shall wait for a SUBACK for the subscription request. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_003:  `EnableMethodsAsync` shall return failure if the subscription request fails. 
                await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(methodPostTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                //SRS_CSHARP_MQTT_TRANSPORT_28_001: `DisableMethodsAsync` shall unsubscribe using the '$iothub/methods/POST/' topic filter.
                //SRS_CSHARP_MQTT_TRANSPORT_28_002: `DisableMethodsAsync` shall wait for a UNSUBACK for the unsubscription.
                //SRS_CSHARP_MQTT_TRANSPORT_28_003: `DisableMethodsAsync` shall return failure if the unsubscription fails.
                await this.channel.WriteAsync(new UnsubscribePacket(0, methodPostTopicFilter)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_020: `EnableEventReceiveAsync` shall open the transport if this method is called when the transport is not open.
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync();
                }

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_021:  `EnableEventReceiveAsync` shall subscribe using the 'devices/{0}/modules/{1}/' topic filter. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_022:  `EnableEventReceiveAsync` shall wait for a SUBACK for the subscription request. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_33_023:  `EnableEventReceiveAsync` shall return failure if the subscription request fails. 
                await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(this.receiveEventMessageFilter, this.qos)));
            }, cancellationToken);
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                //SRS_CSHARP_MQTT_TRANSPORT_33_021: `DisableEventReceiveAsync` shall unsubscribe using the 'devices/{0}/modules/{1}/#' topic filter.
                //SRS_CSHARP_MQTT_TRANSPORT_33_022: `DisableEventReceiveAsync` shall wait for a UNSUBACK for the unsubscription.
                //SRS_CSHARP_MQTT_TRANSPORT_33_023: `DisableEventReceiveAsync` shall return failure if the unsubscription fails.
                await this.channel.WriteAsync(new UnsubscribePacket(0, this.receiveEventMessageFilter));
            }, cancellationToken);
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync().ConfigureAwait(false);
                }
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_005:  `SendMethodResponseAsync` shall allocate a `Message` object containing the method response. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_006:  `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_007:  `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object. 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_008:  `SendMethodResponseAsync` shall send the message to the service. 
                var message = new Message(methodResponse.BodyStream);

                message.MqttTopicName = methodResponseTopic.FormatInvariant(methodResponse.Status, methodResponse.RequestId);

                await this.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_033: `EnableTwinPatchAsync` shall open the transport if this method is called when the transport is not open. 
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync().ConfigureAwait(false);
                }

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_010: `EnableTwinPatchAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_011: `EnableTwinPatchAsync` shall wait for a SUBACK on the subscription request.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_012: `EnableTwinPatchAsync` shall return failure if the subscription request fails.
                await this.channel.WriteAsync(new SubscribePacket(0, new SubscriptionRequest(twinPatchTopicFilter, QualityOfService.AtMostOnce))).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        Boolean parseResponseTopic(string topicName, out string rid, out Int32 status)
        {
            var match = this.twinResponseTopicRegex.Match(topicName);
            if (match.Success)
            {
                status = Convert.ToInt32(match.Groups[1].Value);
#if WINDOWS_UWP
                // TODO: verify that WwwFormUrlDecoder does the same as ParseQueryString
                var decoder = new Windows.Foundation.WwwFormUrlDecoder(match.Groups[2].Value);
                rid = decoder.GetFirstValueByName("$rid");
#else
                rid = HttpUtility.ParseQueryString(match.Groups[2].Value).Get("$rid");
#endif
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
            var responseReceived = new SemaphoreSlim(0);
            Message response = null; ;
            Exception responseException = null;

            Action<Message> onTwinResponse = (Message possibleResponse) =>
            {
                try
                {
                    string receivedRid;
                    Int32 status;

                    if (parseResponseTopic(possibleResponse.MqttTopicName, out receivedRid, out status))
                    {
                        if (rid == receivedRid)
                        {
                            if (status >= 300)
                            {
                                throw new Exception("request " + rid + " returned status " + status.ToString());
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
                    responseException = e;
                    responseReceived.Release();

                }
            };

            try
            {
                this.twinResponseEvent += onTwinResponse;

                await this.SendEventAsync(request, cancellationToken).ConfigureAwait(false);

                await responseReceived.WaitAsync(this.TwinTimeout, cancellationToken).ConfigureAwait(false);

                if (responseException != null)
                {
                    throw responseException;
                }
                else if (response == null)
                {
                    throw new TimeoutException("Response for message " + rid + " not received");
                }
                else
                {
                    return response;
                }
            }
            finally
            {
                twinResponseEvent -= onTwinResponse;
            }

        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            Twin twin = null;
            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_034: `SendTwinGetAsync` shall open the transport if this method is called when the transport is not open. 
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync().ConfigureAwait(false);
                }

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_014:  `SendTwinGetAsync` shall allocate a `Message` object to hold the `GET` request 
                var request = new Message();

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_015:  `SendTwinGetAsync` shall generate a GUID to use as the $rid property on the request 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_016:  `SendTwinGetAsync` shall set the `Message` topic to '$iothub/twin/GET/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated 
                string rid = Guid.NewGuid().ToString(); ;
                request.MqttTopicName = "$iothub/twin/GET/?$rid=" + rid;

                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_017:  `SendTwinGetAsync` shall wait for a response from the service with a matching $rid value 
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_019:  If the response is failed, `SendTwinGetAsync` shall return that failure to the caller.
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_020:  If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error 
                using (var response = await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(false))
                {
                    // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_021:  If the response contains a success code, `SendTwinGetAsync` shall return success to the caller  
                    // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_018:  When a response is received, `SendTwinGetAsync` shall return the Twin object to the caller
                    using (StreamReader reader = new StreamReader(response.GetBodyStream(), System.Text.Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();

                        var props = JsonConvert.DeserializeObject<Microsoft.Azure.Devices.Shared.TwinProperties>(body);

                        twin = new Twin();
                        twin.Properties = props;
                    }
                }
            }, cancellationToken).ConfigureAwait(false);

            return twin;
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {

            this.EnsureValidState();

            await this.HandleTimeoutCancellation(async () =>
            {
                // Codes_SRS_CSHARP_MQTT_TRANSPORT_18_035: `SendTwinPatchAsync` shall shall open the transport if this method is called when the transport is not open. 
                if (!this.TransportIsOpen())
                {
                    await this.OpenAsync().ConfigureAwait(false);
                }

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
                    await SendTwinRequestAsync(request, rid, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        Func<IPAddress, int, Task<IChannel>> CreateChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo)
        {
#if WINDOWS_UWP
            return async (address, port) =>
            {
                PlatformProvider.Platform = new UWPPlatform();

                var eventLoopGroup = new MultithreadEventLoopGroup();

                var streamSocket = new StreamSocket();
                await streamSocket.ConnectAsync(new HostName(iotHubConnectionString.HostName), port.ToString(), SocketProtectionLevel.PlainSocket);
                streamSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                await streamSocket.UpgradeToSslAsync(SocketProtectionLevel.Tls12, new HostName(iotHubConnectionString.HostName));

                var streamSocketChannel = new StreamSocketChannel(streamSocket);

                streamSocketChannel.Pipeline.AddLast(
                    MqttEncoder.Instance, 
                    new MqttDecoder(false, MaxMessageSize),
                    this.mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo));

                streamSocketChannel.Configuration.SetOption(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default);

                await eventLoopGroup.GetNext().RegisterAsync(streamSocketChannel).ConfigureAwait(false);

                this.ScheduleCleanup(() =>
                {
                    EventLoopGroupPool.Release(this.eventLoopGroupKey);
                    return TaskConstants.Completed;
                });

                return streamSocketChannel;
            };
#else
            return (address, port) =>
            {
                IEventLoopGroup eventLoopGroup = EventLoopGroupPool.TakeOrAdd(this.eventLoopGroupKey);

                Func<Stream, SslStream> streamFactory = stream => new SslStream(stream, true, settings.RemoteCertificateValidationCallback);
                var clientTlsSettings = settings.ClientCertificate != null ?
                    new ClientTlsSettings(iotHubConnectionString.HostName, new List<X509Certificate> { settings.ClientCertificate }) :
                    new ClientTlsSettings(iotHubConnectionString.HostName);
                Bootstrap bootstrap = new Bootstrap()
                    .Group(eventLoopGroup)
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
                                this.mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo));
                    }));

                this.ScheduleCleanup(() =>
                {
                    EventLoopGroupPool.Release(this.eventLoopGroupKey);
                    return TaskConstants.Completed;
                });

                return bootstrap.ConnectAsync(address, port);
            };
#endif
        }

        Func<IPAddress, int, Task<IChannel>> CreateWebSocketChannelFactory(IotHubConnectionString iotHubConnectionString, MqttTransportSettings settings, ProductInfo productInfo)
        {
            return async (address, port) =>
            {
                string additionalQueryParams = "";
#if WINDOWS_UWP || NETSTANDARD1_3
                // UWP and NETSTANDARD1_3 implementation doesn't set client certs, so we want to tell the IoT Hub to not ask for them
                additionalQueryParams = "?iothub-no-client-cert=true";
#endif
                IEventLoopGroup eventLoopGroup = EventLoopGroupPool.TakeOrAdd(this.eventLoopGroupKey);

                var websocketUri = new Uri(WebSocketConstants.Scheme + iotHubConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                var websocket = new ClientWebSocket();
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Mqtt);

#if !WINDOWS_UWP // UWP does not support proxies
                // Check if we're configured to use a proxy server
                IWebProxy webProxy = WebRequest.DefaultWebProxy;
                Uri proxyAddress = null;
#if !NETSTANDARD1_3
                proxyAddress = webProxy?.GetProxy(websocketUri);
#endif
                if (!websocketUri.Equals(proxyAddress))
                {
                    // Configure proxy server
                    websocket.Options.Proxy = webProxy;
                }
#endif

                if (settings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(settings.ClientCertificate);
                }
#if !WINDOWS_UWP && !NETSTANDARD1_3 // UseDefaultCredentials is not in UWP and NetStandard
                else
                {
                    websocket.Options.UseDefaultCredentials = true;
                }
#endif

                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

#if WINDOWS_UWP
                PlatformProvider.Platform = new UWPPlatform();
#endif
                var clientChannel = new ClientWebSocketChannel(null, websocket);
                clientChannel
                    .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                    .Option(ChannelOption.AutoRead, false)
                    .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                    .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default)
                    .Pipeline.AddLast(
                        MqttEncoder.Instance,
                        new MqttDecoder(false, MaxMessageSize),
                        this.mqttIotHubAdapterFactory.Create(this, iotHubConnectionString, settings, productInfo));
                await eventLoopGroup.GetNext().RegisterAsync(clientChannel).ConfigureAwait(false);

                this.ScheduleCleanup(() =>
                {
                    EventLoopGroupPool.Release(this.eventLoopGroupKey);
                    return TaskConstants.Completed;
                });

                return clientChannel;
            };
        }

        void ScheduleCleanup(Func<Task> cleanupTask)
        {
            Func<Task> currentCleanupFunc = this.cleanupFunc;
            this.cleanupFunc = async () =>
            {
                await cleanupTask().ConfigureAwait(false);

                if (currentCleanupFunc != null)
                {
                    await currentCleanupFunc().ConfigureAwait(false);
                }
            };
        }

        async void Cleanup()
        {
            try
            {
                await this.closeRetryPolicy.ExecuteAsync(this.CleanupAsync).ConfigureAwait(false);
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
            return TaskConstants.Completed;
        }

        bool TryStateTransition(TransportState fromState, TransportState toState)
        {
            return (TransportState)Interlocked.CompareExchange(ref this.state, (int)toState, (int)fromState) == fromState;
        }

        void EnsureValidState()
        {
            if (this.State == TransportState.Error)
            {
                throw new IotHubClientException(this.fatalException);
            }
            if (this.State == TransportState.Closed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}




