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
    internal class AmqpClientConnectionSasSingle : AmqpClientConnection
    {
        AmqpClientSession authenticationSession;
        AmqpClientSession workerSession;

        internal bool isConnectionClosed;
        private ProtocolHeader sentProtocolHeader;
        private TaskCompletionSource<TransportBase> taskCompletionSource;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        public AmqpClientConnectionSasSingle(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
            : base(deviceClientEndpointIdentity, removeDelegate)
        {
            authenticationSession = null;
            workerSession = null;
        }

        internal override async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            TransportBase transport;

            switch (this.amqpTransportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                    transport = await CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    //transport = await CreateClientWebSocketTransportAsync(timeout, proxy).ConfigureAwait(false);
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

                // Create Sessions
                //authenticationSession = new AmqpClientSession(this);
                workerSession = new AmqpClientSession(this);
                await workerSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                //var amqpLinkFactory = new AmqpLinkFactory();
                //amqpTestLinkFactory.LinkCreated += OnLinkCreated;

                //this.authenticationSession = new AmqpSession(amqpConnection, amqpSettings, )
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

        internal override async Task<Outcome> SendEventAsync(AmqpMessage message, TimeSpan timeout)
        {
            Outcome outcome;

            outcome = await workerSession.SendMessageAsync(message, timeout).ConfigureAwait(false);

            return outcome;
        }

        internal override Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

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

        void OnConnectionClosed(object o, EventArgs args)
        {
            isConnectionClosed = true;
        }






        internal override Task EnableMethodAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task DisableTwinAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task DisposeMessageAsync(string lockToken, Accepted acceptedOutcome, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task<Twin> RoundTripTwinMessage(object amqpMessage, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
