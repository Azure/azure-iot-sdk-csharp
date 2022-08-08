// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotTransport : IDisposable
    {
        private readonly bool _disableServerCertificateValidation;
        private readonly string _hostName;
        private readonly AmqpSettings _amqpSettings;
        private readonly IotHubClientAmqpSettings _amqpTransportSettings;
        private readonly TlsTransportSettings _tlsTransportSettings;

        private ClientWebSocketTransport _clientWebSocketTransport;

        public AmqpIotTransport(
            AmqpSettings amqpSettings,
            IotHubClientAmqpSettings amqpTransportSettings,
            string hostName,
            bool disableServerCertificateValidation)
        {
            _amqpSettings = amqpSettings;
            _amqpTransportSettings = amqpTransportSettings;
            _hostName = hostName;
            _disableServerCertificateValidation = disableServerCertificateValidation;

            var tcpTransportSettings = new TcpTransportSettings
            {
                Host = hostName,
                Port = AmqpConstants.DefaultSecurePort,
            };

            _tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = hostName,
                Certificate = null,
                CertificateValidationCallback = _amqpTransportSettings.RemoteCertificateValidationCallback
                    ?? OnRemoteCertificateValidation,
                Protocols = amqpTransportSettings.SslProtocols,
            };

            if (_amqpTransportSettings.ClientCertificate != null)
            {
                _tlsTransportSettings.Certificate = _amqpTransportSettings.ClientCertificate;
            }
        }

        public void Dispose()
        {
            _clientWebSocketTransport?.Dispose();
            _clientWebSocketTransport = null;
        }

        internal async Task<TransportBase> InitializeAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(InitializeAsync));

            TransportBase transport;

            switch (_amqpTransportSettings.Protocol)
            {
                case TransportProtocol.Tcp:
                    var amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, _tlsTransportSettings);
                    transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case TransportProtocol.WebSocket:
                    transport = _clientWebSocketTransport = (ClientWebSocketTransport)await CreateClientWebSocketTransportAsync(cancellationToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(InitializeAsync));

            return transport;
        }

        private async Task<TransportBase> CreateClientWebSocketTransportAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Logging.IsEnabled)
                    Logging.Enter(this, nameof(CreateClientWebSocketTransportAsync));

                string additionalQueryParams = "";
                var websocketUri = new Uri($"{WebSocketConstants.Scheme}{_hostName}:{WebSocketConstants.SecurePort}{WebSocketConstants.UriSuffix}{additionalQueryParams}");
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
                ClientWebSocket websocket = await CreateClientWebSocketAsync(websocketUri, cancellationToken).ConfigureAwait(false);
                return new ClientWebSocketTransport(websocket, null, null);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(CreateClientWebSocketTransportAsync)}");
            }
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, nameof(CreateClientWebSocketAsync));

                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = _amqpTransportSettings.Proxy;

                try
                {
                    if (webProxy != DefaultWebProxySettings.Instance)
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = webProxy;
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Set ClientWebSocket.Options.Proxy to {webProxy}");
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"{nameof(CreateClientWebSocketAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                }

                if (_amqpTransportSettings.WebSocketKeepAlive.HasValue)
                {
                    websocket.Options.KeepAliveInterval = _amqpTransportSettings.WebSocketKeepAlive.Value;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Set websocket keep-alive to {_amqpTransportSettings.WebSocketKeepAlive}");
                }

                if (_amqpTransportSettings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(_amqpTransportSettings.ClientCertificate);
                }

                await websocket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CreateClientWebSocketAsync));
            }
        }

        private bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (_disableServerCertificateValidation
                && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            if (!_amqpTransportSettings.CertificateRevocationCheck
                && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors
                && CausedByRevocationCheckError(chain))
            {
                return true;
            }

            return false;
        }

        private static bool CausedByRevocationCheckError(X509Chain chain)
        {
            return chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.RevocationStatusUnknown);
        }
    }
}
