// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal sealed class AmqpClientConnection : IDisposable
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private const string UriSuffix = "/$iothub/websocket";
        private const string Scheme = "wss";
        private const string Version = "13";
        private const int WebSocketPort = 443;
        private const int TcpPort = 5671;

        private readonly AmqpSettings _amqpSettings;
        private readonly Uri _uri;
        private readonly Action _onConnectionClosed;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly ProvisioningClientAmqpSettings _clientSettings;

        private TaskCompletionSource<TransportBase> _tcs;
        private TransportBase _transport;
        private ProtocolHeader _sentHeader;

        internal AmqpClientConnection(
            Uri uri,
            AmqpSettings amqpSettings,
            Action onConnectionClosed,
            ProvisioningClientAmqpSettings clientSettings)
        {
            _uri = uri;
            _amqpSettings = amqpSettings;
            _onConnectionClosed = onConnectionClosed;
            _clientSettings = clientSettings;

            AmqpConnectionSettings = new AmqpConnectionSettings
            {
                ContainerId = Guid.NewGuid().ToString(),
                HostName = _uri.Host,
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
                string hostName = _uri.Host;

                var tcpSettings = new TcpTransportSettings
                {
                    Host = hostName,
                    Port = _uri.Port != -1
                    ? _uri.Port
                    : AmqpConstants.DefaultSecurePort,
                };

                TransportSettings = new TlsTransportSettings(tcpSettings)
                {
                    TargetHost = hostName,
                    Certificate = clientCert,
                    CertificateValidationCallback = remoteCerificateValidationCallback,
                    Protocols = _clientSettings.SslProtocols,
                };

                AmqpTransportInitiator amqpTransportInitiator;
                if (useWebSocket)
                {
                    var websocketUri = new Uri($"{Scheme}{_uri.Host}:{WebSocketPort}{UriSuffix}");
                    var websocketTransportSettings = new WebSocketTransportSettings
                    {
                        Uri = websocketUri,
                        Proxy = _clientSettings.Proxy,
                        SubProtocol = Amqpwsb10,
                    };

                    amqpTransportInitiator = new AmqpTransportInitiator(_amqpSettings, websocketTransportSettings);
                }
                else
                {
                    var transportSettings = new TcpTransportSettings
                    {
                        Host = _uri.Host,
                        Port = TcpPort,
                    };

                    var tlsTranpsortSettings = new TlsTransportSettings(transportSettings)
                    {
                        TargetHost = _uri.Host,
                        Certificate = null,
                        CertificateValidationCallback = _clientSettings.RemoteCertificateValidationCallback
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

                SaslTransportProvider provider = _amqpSettings.GetTransportProvider<SaslTransportProvider>();
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
    }
}
