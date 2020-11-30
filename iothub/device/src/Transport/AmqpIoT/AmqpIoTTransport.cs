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
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTTransport : IDisposable
    {
        private readonly bool _disableServerCertificateValidation;
        private readonly string _hostName;
        private readonly AmqpSettings _amqpSettings;
        private readonly AmqpTransportSettings _amqpTransportSettings;
        private readonly TlsTransportSettings _tlsTransportSettings;

        private ClientWebSocketTransport _clientWebSocketTransport;

        public AmqpIoTTransport(
            AmqpSettings amqpSettings,
            AmqpTransportSettings amqpTransportSettings,
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

            SslProtocols protocols = TlsVersions.Instance.Preferred;
#if NET451
            // Requires hardcoding in NET451 otherwise yields error:
            //    System.ArgumentException: The specified value is not valid in the 'SslProtocolType' enumeration.
            if (amqpTransportSettings.GetTransportType() == TransportType.Amqp_Tcp_Only
                && protocols == SslProtocols.None)
            {
                protocols = TlsVersions.Instance.MinimumTlsVersions;
            }
#endif

            _tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = hostName,
                Certificate = null,
                CertificateValidationCallback = _amqpTransportSettings.RemoteCertificateValidationCallback
                    ?? OnRemoteCertificateValidation,
                Protocols = protocols,
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

        internal async Task<TransportBase> InitializeAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, $"{nameof(InitializeAsync)}");

            TransportBase transport;

            switch (_amqpTransportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                    transport = _clientWebSocketTransport = (ClientWebSocketTransport)await CreateClientWebSocketTransportAsync(timeout)
                        .ConfigureAwait(false);
                    break;

                case TransportType.Amqp_Tcp_Only:
                    var amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, _tlsTransportSettings);
                    transport = await amqpTransportInitiator.ConnectTaskAsync(timeout).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            }
            Logging.Exit(this, timeout, $"{nameof(InitializeAsync)}");

            return transport;
        }

        private async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            try
            {
                Logging.Enter(this, timeout, $"{nameof(CreateClientWebSocketTransportAsync)}");

                string additionalQueryParams = "";
                var websocketUri = new Uri($"{WebSocketConstants.Scheme}{_hostName}:{WebSocketConstants.SecurePort}{WebSocketConstants.UriSuffix}{additionalQueryParams}");
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
#if NET451
                if (Environment.OSVersion.Version.Major < 6
                    || (Environment.OSVersion.Version.Major == 6
                        && Environment.OSVersion.Version.Minor <= 1))
                {
                    var websocket = await CreateLegacyClientWebSocketAsync(
                            websocketUri,
                            this._amqpTransportSettings.ClientCertificate,
                            timeout)
                        .ConfigureAwait(false);
                    return new LegacyClientWebSocketTransport(
                        websocket,
                        this._amqpTransportSettings.OperationTimeout,
                        null,
                        null);
                }
                else
                {
#endif
                ClientWebSocket websocket = await CreateClientWebSocketAsync(websocketUri, timeout).ConfigureAwait(false);
                return new ClientWebSocketTransport(websocket, null, null);
#if NET451
                }
#endif
            }
            finally
            {
                Logging.Exit(this, timeout, $"{nameof(CreateClientWebSocketTransportAsync)}");
            }
        }

#if NET451
        private static async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(
            Uri webSocketUri,
            X509Certificate2 clientCertificate,
            TimeSpan timeout)
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            await websocket
                .ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, clientCertificate, timeout)
                .ConfigureAwait(false);
            return websocket;
        }
#endif

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            try
            {
                Logging.Enter(this, timeout, $"{nameof(CreateClientWebSocketAsync)}");

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
                        Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting ClientWebSocket.Options.Proxy");
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    Logging.Error(this, $"{nameof(CreateClientWebSocketAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                }

                if (_amqpTransportSettings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(_amqpTransportSettings.ClientCertificate);
                }

                // Support for RemoteCertificateValidationCallback for ClientWebSocket is introduced in .NET Standard 2.1
#if NETSTANDARD2_1
                if (_amqpTransportSettings.RemoteCertificateValidationCallback != null)
                {
                    websocket.Options.RemoteCertificateValidationCallback = _amqpTransportSettings.RemoteCertificateValidationCallback;
                    Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting RemoteCertificateValidationCallback");
                }
#endif

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                return websocket;
            }
            finally
            {
                Logging.Exit(this, timeout, $"{nameof(CreateClientWebSocketAsync)}");
            }
        }

        private bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (_disableServerCertificateValidation && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            if (!_amqpTransportSettings.CertificateRevocationCheck && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && CausedByRevocationCheckError(chain))
            {
                return true;
            }

            return false;
        }

        private bool CausedByRevocationCheckError(X509Chain chain)
        {
            return chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.RevocationStatusUnknown);
        }
    }
}
