// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpClientConnection
    {
        readonly AmqpSettings _amqpSettings;
        readonly Uri _uri;

        internal AmqpClientConnection(Uri uri, AmqpSettings amqpSettings)
        {
            _uri = uri;
            _amqpSettings = amqpSettings;

            AmqpConnectionSettings = new AmqpConnectionSettings
            {
                ContainerId = Guid.NewGuid().ToString(),
                HostName = _uri.Host
            };
        }

        public AmqpConnection AmqpConnection { get; private set; }

        public AmqpConnectionSettings AmqpConnectionSettings { get; private set; }

        public TlsTransportSettings TransportSettings { get; private set; }

        public AmqpClientSession AmqpSession { get; private set; }

        public bool IsConnectionClosed => _isConnectionClosed;

        private bool _isConnectionClosed;

        public async Task OpenAsync(TimeSpan timeout, bool useWebSocket, X509Certificate2 clientCert)
        {
            var hostName = _uri.Host;

            var tcpSettings = new TcpTransportSettings { Host = hostName, Port = _uri.Port != -1 ? _uri.Port : AmqpConstants.DefaultSecurePort };
            TransportSettings = new TlsTransportSettings(tcpSettings)
            {
                TargetHost = hostName,
                CertificateValidationCallback = (sender, cert, chain, errors) => true,
                Certificate = clientCert
            };

            TransportBase transport;

            if (useWebSocket)
            {
                transport = await CreateClientWebSocketTransportAsync(timeout).ConfigureAwait(false);
            }
            else
            {
                var tcpInitiator = new AmqpTransportInitiator(_amqpSettings, TransportSettings);
                transport = await tcpInitiator.ConnectTaskAsync(timeout).ConfigureAwait(false);
            }

            AmqpConnection = new AmqpConnection(transport, _amqpSettings, AmqpConnectionSettings);
            await AmqpConnection.OpenAsync(timeout).ConfigureAwait(false);
            _isConnectionClosed = false;
            AmqpConnection.Closed += OnConnectionClosed;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            var connection = AmqpConnection;
            if (connection != null)
            {
                await connection.CloseAsync(timeout).ConfigureAwait(false);
            }
        }

        public void Close()
        {
            var connection = AmqpConnection;
            if (connection != null)
            {
                connection.Close();
            }
        }

        public AmqpClientSession CreateSession()
        {
            AmqpSession = new AmqpClientSession(this);

            return AmqpSession;
        }

        void OnConnectionClosed(object o, EventArgs args)
        {
            _isConnectionClosed = true;
        }

        async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            Uri websocketUri = new Uri(WebSocketConstants.Scheme + _uri.Host + ":" + _uri.Port);
            var websocket = await CreateClientWebSocketAsync(websocketUri, timeout).ConfigureAwait(false);
            return new ClientWebSocketTransport(
                websocket,
                null,
                null);
        }

        async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            websocket.Options.KeepAliveInterval = WebSocketConstants.KeepAliveInterval;
            websocket.Options.SetBuffer(WebSocketConstants.BufferSize, WebSocketConstants.BufferSize);

            // TODO: expose the Proxy setting as public API on the transport layer
            //Check if we're configured to use a proxy server
            IWebProxy webProxy = WebRequest.DefaultWebProxy;
            Uri proxyAddress = webProxy != null ? webProxy.GetProxy(websocketUri) : null;
            if (!websocketUri.Equals(proxyAddress))
            {
                // Configure proxy server
                websocket.Options.Proxy = webProxy;
            }

            if (TransportSettings.Certificate != null)
            {
                websocket.Options.ClientCertificates.Add(TransportSettings.Certificate);
            }

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                try
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return websocket;
        }
    }
}
