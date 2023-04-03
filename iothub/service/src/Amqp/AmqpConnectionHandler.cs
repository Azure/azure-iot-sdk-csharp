// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
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
#pragma warning disable CA1852 // used in debug for unit test mocking
    internal class AmqpConnectionHandler : IDisposable
#pragma warning restore CA1852
    {
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new(1, 0, 0);

        private readonly AmqpCbsSessionHandler _cbsSession;
        private readonly AmqpSessionHandler _workerSession;
        private readonly bool _useWebSocketOnly;
        private readonly IotHubServiceClientOptions _options;
        private readonly IotHubConnectionProperties _credential;
        private readonly EventHandler _connectionLossHandler;
        private readonly string _linkAddress;

        // The lock that prevents simultaneous open/close, open/open, and close/close operations
        private readonly SemaphoreSlim _openCloseSemaphore = new(1, 1);

        private AmqpConnection _connection;
        private TransportBase _transport;

        // The current delivery tag. Increments after each send operation to give a unique value.
        private int _sendingDeliveryTag;
        
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected internal AmqpConnectionHandler()
        { }

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        internal AmqpConnectionHandler(
            IotHubConnectionProperties credential,
            IotHubTransportProtocol protocol,
            string linkAddress,
            IotHubServiceClientOptions options,
            EventHandler connectionLossHandler,
            AmqpCbsSessionHandler cbsSession,
            AmqpSessionHandler workerSession)
        {
            _credential = credential;
            _useWebSocketOnly = protocol == IotHubTransportProtocol.WebSocket;
            _linkAddress = linkAddress;
            _options = options;
            _connectionLossHandler = connectionLossHandler;
            _cbsSession = cbsSession;
            _workerSession = workerSession;

            _sendingDeliveryTag = 0;
        }

        internal AmqpConnectionHandler(
            IotHubConnectionProperties credential,
            IotHubTransportProtocol protocol,
            string linkAddress,
            IotHubServiceClientOptions options,
            EventHandler connectionLossHandler,
            Action<AmqpMessage> messageHandler = null)
        {
            _credential = credential;
            _useWebSocketOnly = protocol == IotHubTransportProtocol.WebSocket;
            _linkAddress = linkAddress;
            _options = options;
            _connectionLossHandler = connectionLossHandler;
            _cbsSession = new AmqpCbsSessionHandler(_credential, connectionLossHandler);
            _workerSession = new AmqpSessionHandler(linkAddress, connectionLossHandler, messageHandler);

            _sendingDeliveryTag = 0;
        }

        /// <summary>
        /// Returns true if this connection, its sessions and its sessions' links are all open.
        /// Returns false otherwise.
        /// Marked virtual for unit testing purposes only.
        /// </summary>
        /// <returns>True if this connection, its sessions and its sessions' links are all open. False otherwise.</returns>
        internal virtual bool IsOpen => _connection != null
            && _connection.State == AmqpObjectState.Opened
            && _cbsSession != null
            && _cbsSession.IsOpen()
            && _workerSession != null
            && _workerSession.IsOpen;

        /// <summary>
        /// Opens the AMQP connection. This involves creating the needed TCP or Websocket transport and
        /// then opening all the required sessions and links.
        /// Marked virtual for unit testing purposes only.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal virtual async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Opening amqp connection.", nameof(OpenAsync));

            _openCloseSemaphore.Wait(cancellationToken);

            try
            {
                if (IsOpen)
                {
                    return;
                }

                AmqpSettings amqpSettings = CreateAmqpSettings();

                AmqpTransportInitiator amqpTransportInitiator;
                if (_useWebSocketOnly)
                {
                    var websocketUri = new Uri($"{AmqpsConstants.Scheme}{_credential.HostName}:{AmqpsConstants.WebsocketPort}{AmqpsConstants.UriSuffix}");
                    var websocketTransportSettings = new WebSocketTransportSettings
                    {
                        Uri = websocketUri,
                        Proxy = _options.Proxy,
                        SubProtocol = AmqpsConstants.Amqpwsb10,
                    };

                    amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, websocketTransportSettings);
                }
                else
                {
                    var tcpTransportSettings = new TcpTransportSettings
                    {
                        Host = _credential.HostName,
                        Port = AmqpsConstants.TcpPort,
                    };

                    var tlsTranpsortSettings = new TlsTransportSettings(tcpTransportSettings)
                    {
                        TargetHost = _credential.HostName,
                        Certificate = null,
                        CertificateValidationCallback = _options.RemoteCertificateValidationCallback,
                    };

                    amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTranpsortSettings);
                }

                try
                {
                    _transport = await amqpTransportInitiator.ConnectAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not AuthenticationException)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, ex, nameof(OpenAsync));

                    throw;
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Initialized AMQP transport, ws={_useWebSocketOnly}", nameof(OpenAsync));

                var amqpConnectionSettings = new AmqpConnectionSettings
                {
                    MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                    ContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                    HostName = _credential.HostName,
                    IdleTimeOut = Convert.ToUInt32(_options.AmqpConnectionKeepAlive.TotalMilliseconds)
                };

                _connection = new AmqpConnection(_transport, amqpSettings, amqpConnectionSettings);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(AmqpConnection)} created.", nameof(OpenAsync));

                _connection.Closed += _connectionLossHandler;

                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await _cbsSession.OpenAsync(_connection, cancellationToken).ConfigureAwait(false);
                await _workerSession.OpenAsync(_connection, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticationException authException)
            {
                throw new IotHubServiceException(authException.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, authException);
            }
            catch (SocketException socketException)
            {
                throw new IotHubServiceException(socketException.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, socketException);
            }
            catch (WebSocketException webSocketException)
            {
                if (Fx.ContainsAuthenticationException(webSocketException))
                {
                    throw new IotHubServiceException(webSocketException.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, webSocketException);
                }
                throw new IotHubServiceException(webSocketException.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, webSocketException);
            }
            finally
            {
                _openCloseSemaphore.Release();
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Opening amqp connection.", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Closes the AMQP connection. This closes all the open links and sessions prior to closing the connection.
        /// Marked virtual for unit testing purposes only.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal virtual async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Closing amqp connection.", nameof(CloseAsync));

            _openCloseSemaphore.Wait(cancellationToken);

            try
            {
                _connection.Closed -= _connectionLossHandler;

                _cbsSession?.Close(); // not async because the cbs link type only has a sync close API

                if (_workerSession != null)
                {
                    await _workerSession.CloseAsync(cancellationToken).ConfigureAwait(false);
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (SocketException socketException)
            {
                throw new IotHubServiceException(socketException.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, socketException);
            }
            catch (WebSocketException webSocketException)
            {
                if (Fx.ContainsAuthenticationException(webSocketException))
                {
                    throw new IotHubServiceException(webSocketException.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, webSocketException);
                }
                throw new IotHubServiceException(webSocketException.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, webSocketException);
            }
            finally
            {
                _openCloseSemaphore.Release();
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Closing amqp connection.", nameof(CloseAsync));
            }
        }

        /// <summary>
        /// Send the provided AMQP message. The connection must be opened first.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal virtual async Task<Outcome> SendAsync(AmqpMessage message, CancellationToken cancellationToken)
        {
            ArraySegment<byte> deliveryTag = GetNextDeliveryTag();
            return await _workerSession.SendAsync(message, deliveryTag, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Acknowledge the message received with the provided delivery tag with "Accepted".
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowlege.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal async Task CompleteMessageAsync(ArraySegment<byte> deliveryTag, CancellationToken cancellationToken = default)
        {
            await _workerSession.AcknowledgeMessageAsync(deliveryTag, AmqpConstants.AcceptedOutcome, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Acknowledge the message received with the provided delivery tag with "Released".
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowlege.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal async Task AbandonMessageAsync(ArraySegment<byte> deliveryTag, CancellationToken cancellationToken = default)
        {
            await _workerSession.AcknowledgeMessageAsync(deliveryTag, AmqpConstants.ReleasedOutcome, cancellationToken).ConfigureAwait(false);
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

        /// <inheritdoc/>
        public void Dispose()
        {
            _openCloseSemaphore?.Dispose();
            _cbsSession?.Dispose();
        }
    }
}
