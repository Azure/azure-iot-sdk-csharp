// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the MQTT protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class ProvisioningTransportHandlerMqtt : ProvisioningTransportHandler
    {
        private static readonly MultithreadEventLoopGroup s_eventLoopGroup = new MultithreadEventLoopGroup();

        // TODO: Unify these constants with IoT hub Device client.
        private const int MaxMessageSize = 256 * 1024;

        private const int MqttTcpPort = 8883;
        private const int ReadTimeoutSeconds = 60;

        private const string WsMqttSubprotocol = "mqtt";
        private const string WsScheme = "wss";
        private const int WsPort = 443;

        private ClientWebSocketChannel _webSocketChannel;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerMqtt class using the specified fallback type.
        /// </summary>
        /// <param name="transportFallbackType">The fallback type allowing direct or WebSocket connections.</param>
        public ProvisioningTransportHandlerMqtt(
            TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
            Port = FallbackType == TransportFallbackType.WebSocketOnly ? WsPort : MqttTcpPort;
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// The fallback type. This allows direct or WebSocket connections.
        /// </summary>
        public TransportFallbackType FallbackType { get; private set; }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            TimeSpan timeout)
        {
            if (TimeSpan.Zero.Equals(timeout))
            {
                throw new OperationCanceledException();
            }

            using var cts = new CancellationTokenSource(timeout);
            return await RegisterAsync(message, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            RegistrationOperationStatus operation = null;

            try
            {
                if (message.Authentication is AuthenticationProviderX509 x509Auth)
                {
                    if (FallbackType == TransportFallbackType.TcpWithWebSocketFallback
                        || FallbackType == TransportFallbackType.TcpOnly)
                    {
                        // TODO: Fallback not implemented.
                        operation = await ProvisionOverTcpUsingX509CertificateAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    else if (FallbackType == TransportFallbackType.WebSocketOnly)
                    {
                        operation = await ProvisionOverWssUsingX509CertificateAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new NotSupportedException($"Not supported {nameof(FallbackType)} value: {FallbackType}");
                    }
                }
                else if (message.Authentication is AuthenticationProviderSymmetricKey symmetricKeyAuth)
                {
                    if (FallbackType == TransportFallbackType.TcpWithWebSocketFallback
                        || FallbackType == TransportFallbackType.TcpOnly)
                    {
                        // TODO: Fallback not implemented.
                        operation = await ProvisionOverTcpUsingSymmetricKeyAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    else if (FallbackType == TransportFallbackType.WebSocketOnly)
                    {
                        operation = await ProvisionOverWssUsingSymmetricKeyAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new NotSupportedException($"Not supported {nameof(FallbackType)} value: {FallbackType}");
                    }
                }
                else
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"Invalid {nameof(AuthenticationProvider)} type.");

                    throw new NotSupportedException(
                        $"{nameof(message.Authentication)} must be of type {nameof(AuthenticationProviderX509)} or {nameof(AuthenticationProviderSymmetricKey)}");
                }

                return operation.RegistrationState;
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerMqtt)} threw exception {ex}", nameof(RegisterAsync));

                throw new ProvisioningTransportException($"MQTT transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webSocketChannel?.Dispose();
                _webSocketChannel = null;
            }

            base.Dispose(disposing);
        }

        private Task<RegistrationOperationStatus> ProvisionOverTcpUsingX509CertificateAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Authentication is AuthenticationProviderX509);
            cancellationToken.ThrowIfCancellationRequested();

            X509Certificate2 clientCertificate = ((AuthenticationProviderX509)message.Authentication).GetAuthenticationCertificate();

            var tlsSettings = new ClientTlsSettings(
                SslProtocols,
                true,
                new List<X509Certificate> { clientCertificate },
                message.GlobalDeviceEndpoint);

            return ProvisionOverTcpCommonAsync(message, tlsSettings, cancellationToken);
        }

        private Task<RegistrationOperationStatus> ProvisionOverTcpUsingSymmetricKeyAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Authentication is AuthenticationProviderSymmetricKey);
            cancellationToken.ThrowIfCancellationRequested();

            var tlsSettings = new ClientTlsSettings(
                this.SslProtocols,
                false,
                new List<X509Certificate>(0),
                message.GlobalDeviceEndpoint);

            return ProvisionOverTcpCommonAsync(message, tlsSettings, cancellationToken);
        }

        private async Task<RegistrationOperationStatus> ProvisionOverTcpCommonAsync(
            ProvisioningTransportRegisterRequest message,
            ClientTlsSettings tlsSettings,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<RegistrationOperationStatus>();

            Func<Stream, SslStream> streamFactory = stream => new SslStream(stream, true, RemoteCertificateValidationCallback);

            Bootstrap bootstrap = new Bootstrap()
                .Group(s_eventLoopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Handler(new ActionChannelInitializer<ISocketChannel>(ch =>
                {
                    ch.Pipeline.AddLast(
                        new ReadTimeoutHandler(ReadTimeoutSeconds),
                        new TlsHandler(streamFactory, tlsSettings),
                        MqttEncoder.Instance,
                        new MqttDecoder(isServer: false, maxMessageSize: MaxMessageSize),
                        new LoggingHandler(LogLevel.DEBUG),
                        new ProvisioningChannelHandlerAdapter(message, tcs, cancellationToken));
                }));

            if (Logging.IsEnabled)
                Logging.Associate(bootstrap, this);

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(message.GlobalDeviceEndpoint).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Info(this, $"DNS resolved {addresses.Length} addresses.");

            IChannel channel = null;
            Exception lastException = null;
            foreach (IPAddress address in addresses)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Connecting to {address}.");

                    channel = await bootstrap.ConnectAsync(address, Port).ConfigureAwait(false);
                    break;
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is ConnectException) // We will handle DotNetty.Transport.Channels.ConnectException
                        {
                            lastException = ex;

                            if (Logging.IsEnabled)
                                Logging.Info(this, $"ConnectException trying to connect to {address}: {ex}");

                            return true;
                        }

                        return false; // Let anything else stop the application.
                    });
                }
            }

            if (channel == null)
            {
                string errorMessage = "Cannot connect to Provisioning Service.";

                if (Logging.IsEnabled)
                    Logging.Error(this, errorMessage);

                ExceptionDispatchInfo.Capture(lastException).Throw();
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        private Task<RegistrationOperationStatus> ProvisionOverWssUsingX509CertificateAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Authentication is AuthenticationProviderX509);
            cancellationToken.ThrowIfCancellationRequested();

            X509Certificate2 clientCertificate = ((AuthenticationProviderX509)message.Authentication).GetAuthenticationCertificate();

            return ProvisionOverWssCommonAsync(message, clientCertificate, cancellationToken);
        }

        private Task<RegistrationOperationStatus> ProvisionOverWssUsingSymmetricKeyAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Authentication is AuthenticationProviderSymmetricKey);
            cancellationToken.ThrowIfCancellationRequested();

            return ProvisionOverWssCommonAsync(message, null, cancellationToken);
        }

        private async Task<RegistrationOperationStatus> ProvisionOverWssCommonAsync(
            ProvisioningTransportRegisterRequest message,
            X509Certificate2 clientCertificate,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<RegistrationOperationStatus>();

            var uriBuilder = new UriBuilder(WsScheme, message.GlobalDeviceEndpoint, Port);
            Uri websocketUri = uriBuilder.Uri;

            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WsMqttSubprotocol);
            if (clientCertificate != null)
            {
                websocket.Options.ClientCertificates.Add(clientCertificate);
            }

            // Support for RemoteCertificateValidationCallback for ClientWebSocket is introduced in .NET Standard 2.1
#if NETSTANDARD2_1_OR_GREATER
            if (RemoteCertificateValidationCallback != null)
            {
                websocket.Options.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;
                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(ProvisionOverWssCommonAsync)} Setting RemoteCertificateValidationCallback");
            }
#endif

            // Check if we're configured to use a proxy server
            try
            {
                if (Proxy != DefaultWebProxySettings.Instance)
                {
                    // Configure proxy server
                    websocket.Options.Proxy = Proxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(ProvisionOverWssCommonAsync)} Setting ClientWebSocket.Options.Proxy: {Proxy}");
                }
            }
            catch (PlatformNotSupportedException)
            {
                // .NET Core 2.0 doesn't support WebProxy configuration - ignore this setting.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisionOverWssUsingX509CertificateAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
            }

            await websocket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);

            _webSocketChannel = new ClientWebSocketChannel(null, websocket);
            _webSocketChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default)
                .Pipeline.AddLast(
                    new ReadTimeoutHandler(ReadTimeoutSeconds),
                    MqttEncoder.Instance,
                    new MqttDecoder(false, MaxMessageSize),
                    new LoggingHandler(LogLevel.DEBUG),
                    new ProvisioningChannelHandlerAdapter(message, tcs, cancellationToken));

            await s_eventLoopGroup.RegisterAsync(_webSocketChannel).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
