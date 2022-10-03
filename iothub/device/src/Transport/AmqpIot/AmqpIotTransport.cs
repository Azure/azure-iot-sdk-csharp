// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotTransport : IDisposable
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private const string Scheme = "wss://";
        private const string UriSuffix = "/$iothub/websocket";
        private const string SecurePort = "443";

        private readonly bool _disableServerCertificateValidation;
        private readonly string _hostName;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly AmqpSettings _amqpSettings;
        private readonly IotHubClientAmqpSettings _amqpTransportSettings;
        private readonly TlsTransportSettings _tlsTransportSettings;

        public AmqpIotTransport(
            IConnectionCredentials connectionCredentials,
            AmqpSettings amqpSettings,
            IotHubClientAmqpSettings amqpTransportSettings,
            string hostName,
            bool disableServerCertificateValidation)
        {
            _connectionCredentials = connectionCredentials;
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

            if (_connectionCredentials.Certificate != null)
            {
                _tlsTransportSettings.Certificate = _connectionCredentials.Certificate;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        internal async Task<TransportBase> InitializeAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(InitializeAsync));

            TransportBase transport;
            AmqpTransportInitiator amqpTransportInitiator;

            switch (_amqpTransportSettings.Protocol)
            {
                case IotHubClientTransportProtocol.Tcp:
                    amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, _tlsTransportSettings);
                    break;

                case IotHubClientTransportProtocol.WebSocket:
                    var websocketUri = new Uri($"{Scheme}{_hostName}:{SecurePort}{UriSuffix}");
                    var websocketTransportSettings = new WebSocketTransportSettings
                    {
                        Uri = websocketUri,
                        Proxy = _amqpTransportSettings.Proxy,
                        SubProtocol = Amqpwsb10,
                    };

                    amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, websocketTransportSettings);
                    break;

                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            }
            transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(InitializeAsync));

            return transport;
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
