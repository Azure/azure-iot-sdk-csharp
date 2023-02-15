// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpClientConnection : IDisposable
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private const string UriSuffix = "/$iothub/websocket";
        private const string Scheme = "wss://";
        private const string Version = "13";
        private const int WebSocketPort = 443;
        private const int TcpPort = 5671;
        private const int BufferSize = 8 * 1024;
        private readonly string _host;
        private readonly Action _onConnectionClosed;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly ProvisioningClientAmqpSettings _clientSettings;

        private TaskCompletionSource<TransportBase> _tcs;
        private TransportBase _transport;
        private ProtocolHeader _sentHeader;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected internal AmqpClientConnection()
        { }

        internal AmqpClientConnection(
            string host,
            AmqpSettings amqpSettings,
            Action onConnectionClosed,
            ProvisioningClientAmqpSettings clientSettings)
        {
            _host = host;
            AmqpSettings = amqpSettings;
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

        internal TlsTransportSettings TransportSettings { get; set; }

        internal AmqpClientSession AmqpSession { get; private set; }

        internal AmqpSettings AmqpSettings { get; }

        public void Dispose()
        {
            // We don't know if the transport object is instantiated as a disposable or not
            // We check and dispose if it is.
            if (_transport is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        internal virtual async Task OpenAsync(
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
                string hostName = _host;

                var tcpSettings = new TcpTransportSettings
                {
                    Host = hostName,
                    Port = TcpPort
                };

                TransportSettings = new TlsTransportSettings(tcpSettings)
                {
                    TargetHost = hostName,
                    Certificate = clientCert,
                    CertificateValidationCallback = remoteCerificateValidationCallback ?? OnRemoteCertificateValidation,
                    Protocols = _clientSettings.SslProtocols,
                };

                if (!useWebSocket)
                {
                    var tcpInitiator = new AmqpTransportInitiator(AmqpSettings, TransportSettings);
                    _transport = await tcpInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _transport = await CreateClientWebSocketTransportAsync(proxy, cancellationToken).ConfigureAwait(false);
                    SaslTransportProvider provider = AmqpSettings.GetTransportProvider<SaslTransportProvider>();
                    if (provider != null)
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}: Using SaslTransport");

                        _sentHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
                        using var buffer = new ByteBuffer(new byte[AmqpConstants.ProtocolHeaderSize]);
                        _sentHeader.Encode(buffer);

                        _tcs = new TaskCompletionSource<TransportBase>(TaskCreationOptions.RunContinuationsAsynchronously);

                        var args = new TransportAsyncCallbackArgs();
                        args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                        args.CompletedCallback = OnWriteHeaderComplete;
                        args.Transport = _transport;
                        bool operationPending = _transport.WriteAsync(args);

                        if (Logging.IsEnabled)
                            Logging.Info(
                                this,
                                $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}: " +
                                $"Sent Protocol Header: {_sentHeader} operationPending: {operationPending} completedSynchronously: {args.CompletedSynchronously}");

                        if (!operationPending)
                        {
                            args.CompletedCallback(args);
                        }

                        _transport = await _tcs.Task.ConfigureAwait(false);
                        await _transport.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                AmqpConnection = new AmqpConnection(_transport, AmqpSettings, AmqpConnectionSettings);
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

        private async Task<TransportBase> CreateClientWebSocketTransportAsync(IWebProxy proxy, CancellationToken cancellationToken)
        {
            var webSocketUriBuilder = new UriBuilder
            {
                Scheme = Scheme,
                Host = _host,
                Port = WebSocketPort
            };

            ClientWebSocket websocket = CreateClientWebSocket(proxy);

            await websocket.ConnectAsync(webSocketUriBuilder.Uri, cancellationToken).ConfigureAwait(false);

            return new ClientWebSocketTransport(websocket, null, null);
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
            if (!_clientSettings.CertificateRevocationCheck
                && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors
                && CausedByRevocationCheckError(chain))
            {
                return true;
            }

            // For all other cases, it is a fail.
            return false;
        }

        internal ClientWebSocket CreateClientWebSocket(IWebProxy webProxy)
        {
            // Just return the user-supplied client websocket if they provided one.
            if (_clientSettings?.ClientWebSocket != null)
            {
                return _clientSettings.ClientWebSocket;
            }

            // Otherwise we'll create the client web socket for them.
            var websocket = new ClientWebSocket();

            if (_clientSettings.WebSocketKeepAlive.HasValue)
            {
                websocket.Options.KeepAliveInterval = _clientSettings.WebSocketKeepAlive.Value;
            }

            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(Amqpwsb10);
            websocket.Options.SetBuffer(BufferSize, BufferSize);

            //Check if we're configured to use a proxy server
            try
            {
                if (webProxy != null)
                {
                    // Configure proxy server
                    websocket.Options.Proxy = webProxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(CreateClientWebSocket)} Setting ClientWebSocket.Options.Proxy");
                }
            }
            catch (PlatformNotSupportedException)
            {
                // .NET Core 2.0 doesn't support WebProxy configuration - ignore this setting.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateClientWebSocket)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
            }

            if (TransportSettings.Certificate != null)
            {
                websocket.Options.ClientCertificates.Add(TransportSettings.Certificate);
            }

            return websocket;
        }

        private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnWriteHeaderComplete)}");

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
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            try
            {
                var receivedHeader = new ProtocolHeader();

                using var byteBuffer = new ByteBuffer(args.Buffer, args.Offset, args.Count);
                receivedHeader.Decode(byteBuffer);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Received Protocol Header: {receivedHeader}");

                if (!receivedHeader.Equals(_sentHeader))
                {
                    throw new AmqpException(AmqpErrorCode.NotImplemented, $"The requested protocol version {_sentHeader} is not supported. The supported version is {receivedHeader}");
                }

                SaslTransportProvider provider = AmqpSettings.GetTransportProvider<SaslTransportProvider>();
                TransportBase transport = provider.CreateTransport(args.Transport, true);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Created SaslTransportHandler ");

                _tcs.TrySetResult(transport);
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                CompleteOnException(args);
            }
        }

        private void CompleteOnException(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}");

            if (args.Exception != null && args.Transport != null)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}: Exception thrown {args.Exception.Message}");

                args.Transport.SafeClose(args.Exception);
                args.Transport = null;
                _tcs.TrySetException(args.Exception);
            }
        }

        private static bool CausedByRevocationCheckError(X509Chain chain)
        {
            return chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.RevocationStatusUnknown);
        }
    }
}
