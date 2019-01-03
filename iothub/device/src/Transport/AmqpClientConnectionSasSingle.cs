using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
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
        AmqpSession authenticationSession;
        AmqpSession workerLinksSession;

        public AmqpClientConnectionSasSingle(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
            : base(deviceClientEndpointIdentity, removeDelegate)
        {
            authenticationSession = null;
            workerLinksSession = null;
        }

        internal override async Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);
            cancellationToken.ThrowIfCancellationRequested();

            // TEMP!!!!
            //switch (this.amqpTransportSettings.GetTransportType())
            //{
            //    case TransportType.Amqp_WebSocket_Only:
            //        transport = await this.CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            //        break;
            //    case TransportType.Amqp_Tcp_Only:
            //        TlsTransportSettings tlsTransportSettings = this.CreateTlsTransportSettings();
            //        var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
            //        transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            //        break;
            //    default:
            //        throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            //}

            AmqpTransportInitiator amqpTransportInitiator = new AmqpTransportInitiator(this.amqpSettings, this.tlsTransportSettings);
            TransportBase transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

            this.amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

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

        internal override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
