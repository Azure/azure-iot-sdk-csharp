// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal sealed class AmqpIotTransport : IDisposable
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private const string Scheme = "wss://";
        private const string UriSuffix = "/$iothub/websocket";
        private const string SecurePort = "443";

        private readonly string _hostName;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly AmqpSettings _amqpSettings;
        private readonly IotHubClientAmqpSettings _amqpTransportSettings;
        private readonly TlsTransportSettings _tlsTransportSettings;
        private ClientWebSocket _websocket;

        private ClientWebSocketTransport _clientWebSocketTransport;

        public AmqpIotTransport(
            IConnectionCredentials connectionCredentials,
            AmqpSettings amqpSettings,
            IotHubClientAmqpSettings amqpTransportSettings,
            string hostName)
        {
            _connectionCredentials = connectionCredentials;
            _amqpSettings = amqpSettings;
            _amqpTransportSettings = amqpTransportSettings;
            _hostName = hostName;

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

            if (_connectionCredentials.ClientCertificate != null)
            {
                _tlsTransportSettings.Certificate = _connectionCredentials.ClientCertificate;
            }
        }

        public void Dispose()
        {
            _clientWebSocketTransport?.Dispose();
            _websocket?.Dispose();
            _clientWebSocketTransport = null;
        }

        internal async Task<TransportBase> InitializeAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(InitializeAsync));

            TransportBase transport;

            switch (_amqpTransportSettings.Protocol)
            {
                case IotHubClientTransportProtocol.Tcp:
                    var amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, _tlsTransportSettings);
                    transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case IotHubClientTransportProtocol.WebSocket:
                    transport = _clientWebSocketTransport = (ClientWebSocketTransport)await CreateClientWebSocketTransportAsync(cancellationToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify either web socket or TCP.");
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
                var websocketUri = new Uri($"{Scheme}{_hostName}:{SecurePort}{UriSuffix}{additionalQueryParams}");
                _websocket = _amqpTransportSettings.ClientWebSocket ?? CreateClientWebSocket();

                await _websocket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);
                return new ClientWebSocketTransport(_websocket, null, null);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(CreateClientWebSocketTransportAsync)}");
            }
        }

        private ClientWebSocket CreateClientWebSocket()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, nameof(CreateClientWebSocket));

                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(Amqpwsb10);

                if (_amqpTransportSettings.Proxy != null)
                {
                    try
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = _amqpTransportSettings.Proxy;
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"{nameof(CreateClientWebSocket)} Set ClientWebSocket.Options.Proxy to {_amqpTransportSettings.Proxy}");
                    }
                    catch (PlatformNotSupportedException ex)
                    {
                        websocket.Dispose();
                        // Some .NET runtimes don't support this property.
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"{nameof(CreateClientWebSocket)} PlatformNotSupportedException thrown as this framework doesn't support proxy.");
                        throw new InvalidOperationException("The current .NET runtime does not support setting the proxy.", ex);
                    }
                }

                if (_amqpTransportSettings.WebSocketKeepAlive.HasValue)
                {
                    websocket.Options.KeepAliveInterval = _amqpTransportSettings.WebSocketKeepAlive.Value;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(CreateClientWebSocket)} Set websocket keep-alive to {_amqpTransportSettings.WebSocketKeepAlive}");
                }

                if (_connectionCredentials.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(_connectionCredentials.ClientCertificate);
                }

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CreateClientWebSocket));
            }
        }

        private bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If there are no policy errors then return the remote certificate validation is a pass.
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // If there are remote certificate chain errors due to unknown revocation status check, then it is a pass only if
            // remote certificate revocation check has been turned off.
            if (!_amqpTransportSettings.CertificateRevocationCheck
                && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors
                && CausedByRevocationCheckError(chain))
            {
                return true;
            }

            // For all other cases, it is a fail.
            return false;
        }

        private static bool CausedByRevocationCheckError(X509Chain chain)
        {
            return chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.RevocationStatusUnknown);
        }
    }
}
