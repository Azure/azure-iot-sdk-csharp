using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionSasSingle : AmqpClientConnection, IDisposable
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = false;

        AmqpClientSession authenticationSession;
        AmqpClientSession workerAmqpClientSession;

        internal bool isConnectionClosed;
        private ProtocolHeader sentProtocolHeader;
        private TaskCompletionSource<TransportBase> taskCompletionSource;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal bool isConnectionAuthenticated { get; private set; }

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);

        private AmqpTokenRefresher amqpTokenRefresher;

        private DeviceClientEndpointIdentity deviceClientEndpointIdentity;

        internal AmqpClientConnectionSasSingle(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasSingle))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}." + "accepts only Sas Single device identities");
            }

            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;

            authenticationSession = null;
            workerAmqpClientSession = null;
            isConnectionAuthenticated = false;
        }

        internal override bool AddToMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            return false;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);
            amqpTokenRefresher?.Cancel();

            TransportBase transport;

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

            amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
            amqpConnection.Closed += OnConnectionClosed;

            try
            {
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                isConnectionClosed = false;

                // Create Session for Authentication
                authenticationSession = new AmqpClientSession(this);
                await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                authenticationSession.OnAmqpClientSessionClosed += AuthenticationSession_OnAmqpClientSessionClosed;

                // Authenticate connection with Cbs
                if (!useLinkBasedTokenRefresh)
                {
                    if (this.amqpTransportSettings.ClientCertificate == null)
                    {
                        this.amqpTokenRefresher = new AmqpTokenRefresher(
                           this.authenticationSession,
                           deviceClientEndpointIdentity.iotHubConnectionString,
                           deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri
                           );

                        // Send Cbs token for new connection first
                        try
                        {
                            await this.amqpTokenRefresher.RefreshTokenAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        }
                        catch (Exception exception) when (!exception.IsFatal())
                        {
                            authenticationSession.amqpSession?.Connection.SafeClose();

                            throw;
                        }
                    }
                    isConnectionAuthenticated = true;
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
            isConnectionClosed = true;
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

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await workerAmqpClientSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            // Send the message
            outcome = await workerAmqpClientSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTwinMessageAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                await workerAmqpClientSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(ReceiveAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Message message;
            AmqpMessage amqpMessage;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            amqpMessage = await workerAmqpClientSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);

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

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (workerAmqpClientSession != null)
            {
                disposeOutcome = await workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeTwinPatchDelivery)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);

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

        public void Dispose()
        {
            amqpTokenRefresher.Dispose();
        }
        #endregion
    }
}
