using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionSasMux : AmqpClientConnection
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = true;

        private AmqpClientSession authenticationSession;

        private class MuxWorker
        {
            internal AmqpClientSession workerAmqpClientSession = null;
        }
        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker> muxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker>();

        private ProtocolHeader sentProtocolHeader;
        private TaskCompletionSource<TransportBase> taskCompletionSource;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);

        internal AmqpClientConnectionSasMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}." + "accepts only SasMux device identities" );
            }

            authenticationSession = null;
        }

        internal override bool AddToMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AddToMux)}");

            bool retVal = false;
            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                if (muxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxWorker()))
                {
                    retVal = true;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AddToMux)}");

            return retVal;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            TransportBase transport;

            // Create transport
            if (amqpConnection == null)
            {
                switch (this.amqpTransportSettings.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                        transport = await CreateClientWebSocketTransportAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        SaslTransportProvider provider = amqpSettings.GetTransportProvider<SaslTransportProvider>();
                        if (provider != null)
                        {
                            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}: Using SaslTransport");
                            sentProtocolHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
                            ByteBuffer buffer = new ByteBuffer(new byte[AmqpConstants.ProtocolHeaderSize]);
                            sentProtocolHeader.Encode(buffer);

                            taskCompletionSource = new TaskCompletionSource<TransportBase>();

                            var args = new TransportAsyncCallbackArgs();
                            args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                            args.CompletedCallback = OnWriteHeaderComplete;
                            args.Transport = transport;
                            bool operationPending = transport.WriteAsync(args);

                            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}: Sent Protocol Header: {sentProtocolHeader.ToString()} operationPending: {operationPending} completedSynchronously: {args.CompletedSynchronously}");

                            if (!operationPending)
                            {
                                args.CompletedCallback(args);
                            }

                            transport = await taskCompletionSource.Task.ConfigureAwait(false);
                            await transport.OpenAsync(timeout).ConfigureAwait(false);
                        }
                        break;
                    case TransportType.Amqp_Tcp_Only:
                        var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                        transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        break;
                    default:
                        throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
                }
                try
                {
                    // Create connection from transport
                    if (amqpConnection == null)
                    {
                        amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                        amqpConnection.Closed += OnConnectionClosed;
                        await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    }

                    // Create Session for Authentication
                    if (authenticationSession == null)
                    {
                        authenticationSession = new AmqpClientSession(this);
                        await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        authenticationSession.OnAmqpClientSessionClosed += AuthenticationSession_OnAmqpClientSessionClosed;
                    }
                }
                catch (Exception ex) // when (!ex.IsFatal())
                {
                    if (amqpConnection.TerminalException != null)
                    {
                        throw AmqpClientHelper.ToIotHubClientContract(amqpConnection.TerminalException);
                    }

                    amqpConnection.SafeClose(ex);
                    throw;
                }
                finally
                {
                    if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");
                }
            }
        }

        private void AuthenticationSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AuthenticationSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void WorkerAmqpClientSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(WorkerAmqpClientSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OnConnectionClosed)}");
            muxedDevices.Clear();
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(CloseAsync)}");

            if (amqpConnection != null)
            {
                await amqpConnection.CloseAsync(timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(CloseAsync)}");
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTelemetryAndC2DAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession == null)
                {
                    muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                    await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await muxWorker.workerAmqpClientSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTelemetryAndC2DAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTelemetryAndC2DAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            // Send the message
            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    outcome = await muxWorker.workerAmqpClientSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTelemetrMessageAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession == null)
                {
                    muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                    await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await muxWorker.workerAmqpClientSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableMethodsAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableMethodsAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    outcome = await muxWorker.workerAmqpClientSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendMethodResponseAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession == null)
                {
                    muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                    await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await muxWorker.workerAmqpClientSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTwinPatchAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTwinAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTwinMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    outcome = await muxWorker.workerAmqpClientSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTwinMessageAsync)}" + "TryGetValue failed");
            }


            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableEventsReceiveAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(ReceiveAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Message message;
            AmqpMessage amqpMessage = null;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    amqpMessage = await muxWorker.workerAmqpClientSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(ReceiveAsync)}" + "TryGetValue failed");
            }

            if (amqpMessage != null)
            {
                message = new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            else
            {
                message = null;
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(ReceiveAsync)}");

            return message;
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeMessageAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    disposeOutcome = await muxWorker.workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeTwinPatchDelivery)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    muxWorker.workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}" + "TryGetValue failed");
            }


            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeTwinPatchDelivery)}");
        }
        #endregion

        #region Helpers
        private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnWriteHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            byte[] headerBuffer = new byte[AmqpConstants.ProtocolHeaderSize];
            args.SetBuffer(headerBuffer, 0, headerBuffer.Length);
            args.CompletedCallback = OnReadHeaderComplete;
            bool operationPending = args.Transport.ReadAsync(args);

            if (!operationPending)
            {
                args.CompletedCallback(args);
            }
        }

        private void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            try
            {
                ProtocolHeader receivedHeader = new ProtocolHeader();
                receivedHeader.Decode(new ByteBuffer(args.Buffer, args.Offset, args.Count));

                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Received Protocol Header: {receivedHeader.ToString()}");

                if (!receivedHeader.Equals(sentProtocolHeader))
                {
                    throw new AmqpException(AmqpErrorCode.NotImplemented, $"The requested protocol version {sentProtocolHeader} is not supported. The supported version is {receivedHeader}");
                }

                SaslTransportProvider provider = amqpSettings.GetTransportProvider<SaslTransportProvider>();
                var transport = provider.CreateTransport(args.Transport, true);
                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Created SaslTransportHandler ");
                taskCompletionSource.TrySetResult(transport);
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                CompleteOnException(args);
            }
        }

        private void CompleteOnException(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}");

            if (args.Exception != null && args.Transport != null)
            {
                if (Logging.IsEnabled) Logging.Error(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}: Exception thrown {args.Exception.Message}");

                args.Transport.SafeClose(args.Exception);
                args.Transport = null;
                taskCompletionSource.TrySetException(args.Exception);
            }
        }
        #endregion
    }
}
