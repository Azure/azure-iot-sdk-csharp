// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal sealed class AmqpClientConnection : IDisposable
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private const string UriSuffix = "/$iothub/websocket";
        private const string Scheme = "wss://";
        private const string Version = "13";
        private const int WebSocketPort = 443;
        private const int TcpPort = 5671;

        private readonly AmqpSettings _amqpSettings;
        private readonly string _host;
        private readonly Action _onConnectionClosed;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly ProvisioningClientAmqpSettings _clientSettings;

        private TransportBase _transport;

        internal AmqpClientConnection(
            string host,
            AmqpSettings amqpSettings,
            Action onConnectionClosed,
            ProvisioningClientAmqpSettings clientSettings)
        {
            _host = host;
            _amqpSettings = amqpSettings;
            _onConnectionClosed = onConnectionClosed;
            _clientSettings = clientSettings;

            AmqpConnectionSettings = new AmqpConnectionSettings
            {
                ContainerId = Guid.NewGuid().ToString(),
                HostName = _host,
                IdleTimeOut = Convert.ToUInt32(_clientSettings.IdleTimeout.TotalMilliseconds),
            };
        }

        internal AmqpConnection AmqpConnection { get; private set; }

        internal AmqpConnectionSettings AmqpConnectionSettings { get; private set; }

        internal TlsTransportSettings TransportSettings { get; private set; }

        internal AmqpClientSession AmqpSession { get; private set; }

        public void Dispose()
        {
            // We don't know if the transport object is instantiated as a disposable or not
            // We check and dispose if it is.
            if (_transport is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        internal async Task OpenAsync(
            bool useWebSocket,
            X509Certificate2 clientCert,
            IWebProxy proxy,
            RemoteCertificateValidationCallback remoteCerificateValidationCallback,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}");

            await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var tcpSettings = new TcpTransportSettings
                {
                    Host = _host,
                    Port = TcpPort,
                };

                TransportSettings = new TlsTransportSettings(tcpSettings)
                {
                    TargetHost = _host,
                    CertificateValidationCallback = remoteCerificateValidationCallback,
                };

                AmqpTransportInitiator amqpTransportInitiator;
                if (useWebSocket)
                {
                    var websocketUri = new Uri($"{Scheme}{_host}:{WebSocketPort}{UriSuffix}");
                    var websocketTransportSettings = new WebSocketTransportSettings
                    {
                        Uri = websocketUri,
                        Proxy = _clientSettings.Proxy,
                        SubProtocol = Amqpwsb10,
                    };

                    //TODO certificates can't be passed in, so this will fail with x509 auth.

                    amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, websocketTransportSettings);
                }
                else
                {
                    var tcpTransportSettings = new TcpTransportSettings
                    {
                        Host = _host,
                        Port = TcpPort,
                    };

                    var tlsTranpsortSettings = new TlsTransportSettings(tcpTransportSettings)
                    {
                        TargetHost = _host,
                        Certificate = clientCert,
                        CertificateValidationCallback = _clientSettings.RemoteCertificateValidationCallback,
                        Protocols = _clientSettings.SslProtocols,
                    };

                    amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, tlsTranpsortSettings);
                }

                _transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);

                AmqpConnection = new AmqpConnection(_transport, _amqpSettings, AmqpConnectionSettings);
                AmqpConnection.Closed += OnConnectionClosed;
                await AmqpConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _connectionSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}");
            }
        }

        internal async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (AmqpConnection == null)
            {
                return;
            }

            await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await AmqpConnection.CloseAsync(cancellationToken).ConfigureAwait(false);
                AmqpConnection = null;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        internal void Close()
        {
            AmqpConnection connection = AmqpConnection;
            if (connection != null)
            {
                connection.Close();
            }
        }

        internal AmqpClientSession CreateSession()
        {
            AmqpSession = new AmqpClientSession(this);

            return AmqpSession;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Error(this, $"AMQP connection was lost.");

            _onConnectionClosed.Invoke();
        }
    }
}
