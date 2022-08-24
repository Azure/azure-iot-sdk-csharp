// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Amqp
{
    /// <summary>
    /// Handles a single AMQP connection. Supports TCP and Websocket ports.
    /// </summary>
    /// <remarks>
    /// This class intentionally abstracts away details about sessions and links for simplicity at the service client level.
    /// </remarks>
    internal class AmqpConnectionHandler
    {
        private AmqpConnection _connection;
        private AmqpCbsSessionHandler _cbsSession;
        private AmqpSessionHandler _workerSession;
        private TransportBase _transport;
        private readonly bool _useWebSocketOnly;
        private readonly IotHubServiceClientOptions _options;
        private IotHubConnectionProperties _credential;
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new(1, 0, 0);

        private EventHandler _connectionLossHandler;

        private string _linkAddress;

        // The current delivery tag. Increments after each send operation to give a unique value.
        private int _sendingDeliveryTag;

        public AmqpConnectionHandler(
            IotHubConnectionProperties credential,
            bool useWebSocketOnly,
            string linkAddress,
            IotHubServiceClientOptions options,
            EventHandler connectionLossHandler,
            Action<AmqpMessage> messageHandler = null)
        {
            _credential = credential;
            _useWebSocketOnly = useWebSocketOnly;
            _linkAddress = linkAddress;
            _options = options;
            _connectionLossHandler = connectionLossHandler;
            _cbsSession = new AmqpCbsSessionHandler(_credential, connectionLossHandler);
            _workerSession = new AmqpSessionHandler(linkAddress, connectionLossHandler, messageHandler);

            _sendingDeliveryTag = 0;
        }

        /// <summary>
        /// Opens the AMQP connection. This involves creating the needed TCP or Websocket transport and
        /// then opening all the required sessions and links.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening amqp connection.");

            try
            {
                AmqpSettings amqpSettings = CreateAmqpSettings();

                if (_useWebSocketOnly)
                {
                    // Try only AMQP transport over WebSocket
                    _transport = await CreateClientWebSocketTransportAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    TlsTransportSettings tlsTransportSettings = CreateTlsTransportSettings();
                    var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                    try
                    {
                        _transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (!(e is AuthenticationException))
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, e, nameof(OpenAsync));

                        throw;
                    }
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Initialized AMQP transport, ws={_useWebSocketOnly}");

                var amqpConnectionSettings = new AmqpConnectionSettings
                {
                    MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                    ContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                    HostName = _credential.AmqpEndpoint.Host,
                    IdleTimeOut = Convert.ToUInt32(_options.AmqpConnectionKeepAlive.TotalMilliseconds)
                };

                _connection = new AmqpConnection(_transport, amqpSettings, amqpConnectionSettings);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(AmqpConnection)} created.");

                _connection.Closed += _connectionLossHandler;

                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await _cbsSession.OpenAsync(_connection, cancellationToken).ConfigureAwait(false);
                await _workerSession.OpenAsync(_connection, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening amqp connection.");
            }
        }

        /// <summary>
        /// Closes the AMQP connection. This closes all the open links and sessions prior to closing the connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing amqp connection.");

            try
            {
                _cbsSession.Close(); // not async because the cbs link type only has a sync close API
                await _workerSession.CloseAsync(cancellationToken).ConfigureAwait(false);
                await _connection.CloseAsync(cancellationToken).ConfigureAwait(false);

                if (_transport is ClientWebSocketTransport webSocketTransport)
                {
                    // This is the one disposable object in the entire AMQP stack. It is safe to dispose this
                    // in the close operation since a new websocket transport is created upon each newly
                    // opened AMQP connection.
                    webSocketTransport.Dispose();
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing amqp connection.");
            }
        }

        /// <summary>
        /// Send the provided AMQP message. The connection must be opened first.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<Outcome> SendAsync(AmqpMessage message, CancellationToken cancellationToken)
        {
            ArraySegment<byte> deliveryTag = GetNextDeliveryTag();
            return await _workerSession.SendAsync(message, deliveryTag, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Acknowledge the message received with the provided delivery tag with "Accepted".
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowlege.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task CompleteMessageAsync(ArraySegment<byte> deliveryTag, CancellationToken cancellationToken = default)
        {
            await _workerSession.AcknowledgeMessageAsync(deliveryTag, AmqpConstants.AcceptedOutcome, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Acknowledge the message received with the provided delivery tag with "Released".
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowlege.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task AbandonMessageAsync(ArraySegment<byte> deliveryTag, CancellationToken cancellationToken = default)
        {
            await _workerSession.AcknowledgeMessageAsync(deliveryTag, AmqpConstants.ReleasedOutcome, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns true if this connection, its sessions and its sessions' links are all open.
        /// Returns false otherwise.
        /// </summary>
        /// <returns>True if this connection, its sessions and its sessions' links are all open. False otherwise.</returns>
        public bool IsOpen()
        {
            return _connection != null
                && _connection.State == AmqpObjectState.Opened
                && _cbsSession != null
                && _cbsSession.IsOpen()
                && _workerSession != null
                && _workerSession.IsOpen();
        }

        private ArraySegment<byte> GetNextDeliveryTag()
        {
            int nextDeliveryTag = Interlocked.Increment(ref _sendingDeliveryTag);
            return new ArraySegment<byte>(BitConverter.GetBytes(nextDeliveryTag));
        }

        private static AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            if (Logging.IsEnabled)
                Logging.Info(s_amqpVersion_1_0_0, nameof(CreateAmqpSettings));

            return amqpSettings;
        }

        private TlsTransportSettings CreateTlsTransportSettings()
        {
            var tcpTransportSettings = new TcpTransportSettings
            {
                Host = _credential.HostName,
                Port = _credential.AmqpEndpoint.Port,
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = _credential.HostName,
                Certificate = null,
                CertificateValidationCallback = OnRemoteCertificateValidation
            };

            if (Logging.IsEnabled)
                Logging.Info($"host={tcpTransportSettings.Host}, port={tcpTransportSettings.Port}", nameof(CreateTlsTransportSettings));

            return tlsTransportSettings;
        }

        private static bool OnRemoteCertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        private async Task<ClientWebSocketTransport> CreateClientWebSocketTransportAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(CreateClientWebSocketTransportAsync));

            try
            {
                var websocketUri = new Uri($"{AmqpsConstants.Scheme}{_credential.HostName}:{AmqpsConstants.SecurePort}{AmqpsConstants.UriSuffix}");

                if (Logging.IsEnabled)
                    Logging.Info(this, websocketUri, nameof(CreateClientWebSocketTransportAsync));

                ClientWebSocket websocket = await CreateClientWebSocketAsync(websocketUri, cancellationToken).ConfigureAwait(false);
                return new ClientWebSocketTransport(websocket, null, null);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CreateClientWebSocketTransportAsync));
            }
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, websocketUri, cancellationToken, nameof(CreateClientWebSocketAsync));

            try
            {
                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);

                if (_options.AmqpWebSocketKeepAlive != null)
                {
                    // safe to cast from TimeSpan? to TimeSpan since it is not null
                    websocket.Options.KeepAliveInterval = (TimeSpan)_options.AmqpWebSocketKeepAlive;
                }

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = _options.Proxy;

                try
                {
                    if (webProxy != DefaultWebProxySettings.Instance)
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = webProxy;
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting ClientWebSocket.Options.Proxy");
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"{nameof(CreateClientWebSocketAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                }

                await websocket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, websocketUri, cancellationToken, nameof(CreateClientWebSocketAsync));
            }
        }
    }
}
