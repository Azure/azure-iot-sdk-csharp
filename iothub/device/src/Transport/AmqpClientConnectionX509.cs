using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionX509 : AmqpClientConnection
    {
        #region Members-Constructor
        AmqpClientSession workerAmqpClientSession;

        internal bool isConnectionClosed;
        private ProtocolHeader sentProtocolHeader;
        private TaskCompletionSource<TransportBase> taskCompletionSource;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal bool isConnectionAuthenticated { get; private set; }

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        public AmqpClientConnectionX509(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
            : base(deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}");

            workerAmqpClientSession = null;
            isConnectionAuthenticated = false;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            TransportBase transport;

            switch (this.amqpTransportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                    transport = await CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    SaslTransportProvider provider = amqpSettings.GetTransportProvider<SaslTransportProvider>();
                    if (provider != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}: Using SaslTransport");
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

                isConnectionAuthenticated = true;
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
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}");
            }
        }

        private void AuthenticationSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(AuthenticationSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void WorkerAmqpClientSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(WorkerAmqpClientSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OnConnectionClosed)}");
            isConnectionClosed = true;
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(CloseAsync)}");

            if (amqpConnection != null)
            {
                await amqpConnection.CloseAsync(timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(CloseAsync)}");
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTelemetryAndC2DAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await workerAmqpClientSession.OpenLinkTelemetryAndC2DAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override Task DisableTelemetryAndC2DAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTelemetrMessageAsync)}");

            Outcome outcome;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(timeout).ConfigureAwait(false);

            // Send the message
            outcome = await workerAmqpClientSession.SendTelemetryMessageAsync(message, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableMethodsAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkMethodsAsync(correlationid, methodReceivedListener, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(TimeSpan timeout)
        {
            await workerAmqpClientSession.CloseLinkMethodsAsync(timeout).ConfigureAwait(false);
        }

        internal override async Task<Outcome> SendMethodResponseAsync(AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendMethodResponseAsync(methodResponse, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTwinPatchAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkTwinAsync(correlationid, onTwinPathReceivedListener, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(TimeSpan timeout)
        {
            await workerAmqpClientSession.CloseLinkTwinAsync(timeout).ConfigureAwait(false);
        }

        internal override async Task<Outcome> SendTwinMessageAsync(AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTwinMessageAsync)}");

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendTwinMessageAsync(twinMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableEventsReceiveAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                await workerAmqpClientSession.OpenLinkEventsAsync(onEventsReceivedListener, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(ReceiveAsync)}");

            Message message;
            AmqpMessage amqpMessage;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(timeout).ConfigureAwait(false);

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(ReceiveAsync)}");

            return message;
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeMessageAsync)}");

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (workerAmqpClientSession != null)
            {
                disposeOutcome = await workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeTwinPatchDelivery)}");

            workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeTwinPatchDelivery)}");
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