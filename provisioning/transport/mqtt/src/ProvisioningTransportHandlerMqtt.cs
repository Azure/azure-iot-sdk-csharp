// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents the MQTT protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class ProvisioningTransportHandlerMqtt : ProvisioningTransportHandler
    {
        private static MultithreadEventLoopGroup s_eventLoopGroup = new MultithreadEventLoopGroup();

        // TODO: Unify these constants with IoT Hub Device client.
        private const int MaxMessageSize = 256 * 1024;
        private const int MqttTcpPort = 8883;
        private const int ReadTimeoutSeconds = 60;

        private const string WsMqttSubprotocol = "mqtt";
        private const string WsScheme = "wss";
        private const int WsPort = 443;

        /// <summary>
        /// The fallback type. This allows direct or WebSocket connections.
        /// </summary>
        public TransportFallbackType FallbackType { get; private set; }

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerMqtt class using the specified fallback type.
        /// </summary>
        /// <param name="transportFallbackType">The fallback type allowing direct or WebSocket connections.</param>
        public ProvisioningTransportHandlerMqtt(
            TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
            if (FallbackType == TransportFallbackType.WebSocketOnly) 
            {
                Port = WsPort;
            }
            else
            {
                Port = MqttTcpPort;
            }
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");

            cancellationToken.ThrowIfCancellationRequested();

            SecurityProviderX509 security;

            try
            {
                if (message.Security is SecurityProviderX509)
                {
                    security = (SecurityProviderX509)message.Security;
                }
                else
                {
                    if (Logging.IsEnabled) Logging.Error(this, $"Invalid {nameof(SecurityProvider)} type.");
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityProviderX509)}");
                }

                RegistrationOperationStatus operation = null;

                if (FallbackType == TransportFallbackType.TcpWithWebSocketFallback ||
                    FallbackType == TransportFallbackType.TcpOnly)
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

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"{nameof(ProvisioningTransportHandlerMqtt)} threw exception {ex}",
                    nameof(RegisterAsync));

                throw new ProvisioningTransportException($"MQTT transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerMqtt)}.{nameof(RegisterAsync)}");
            }
        }

        private static DeviceRegistrationResult ConvertToProvisioningRegistrationResult(Models.DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new DeviceRegistrationResult(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag);
        }

        private async Task<RegistrationOperationStatus> ProvisionOverTcpUsingX509CertificateAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Security is SecurityProviderX509);
            cancellationToken.ThrowIfCancellationRequested();

            X509Certificate2 clientCertificate =
                ((SecurityProviderX509)message.Security).GetAuthenticationCertificate();

            var tlsSettings = new ClientTlsSettings(
                message.GlobalDeviceEndpoint,
                new List<X509Certificate> { clientCertificate });

            var tcs = new TaskCompletionSource<RegistrationOperationStatus>();

            Bootstrap bootstrap = new Bootstrap()
                .Group(s_eventLoopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Handler(new ActionChannelInitializer<ISocketChannel>(ch =>
                {
                    ch.Pipeline.AddLast(
                        new ReadTimeoutHandler(ReadTimeoutSeconds),
                        new TlsHandler(tlsSettings), //TODO: Ensure SystemDefault is used.
                        MqttEncoder.Instance,
                        new MqttDecoder(isServer: false, maxMessageSize: MaxMessageSize),
                        new ProvisioningChannelHandlerAdapter(message, tcs, cancellationToken));
                }));

            if (Logging.IsEnabled) Logging.Associate(bootstrap, this);

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(message.GlobalDeviceEndpoint).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Info(this, $"DNS resolved {addresses.Length} addresses.");

            IChannel channel = null;
            Exception lastException = null;
            foreach (IPAddress address in addresses)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (Logging.IsEnabled) Logging.Info(this, $"Connecting to {address.ToString()}.");
                    channel = await bootstrap.ConnectAsync(address, Port).ConfigureAwait(false);
                }
                catch (TimeoutException ex)
                {
                    lastException = ex;
                    if (Logging.IsEnabled) Logging.Info(
                        this,
                        $"TimeoutException trying to connect to {address.ToString()}: {ex.ToString()}");
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    if (Logging.IsEnabled) Logging.Info(
                        this,
                        $"IOException trying to connect to {address.ToString()}: {ex.ToString()}");
                }
            }

            if (channel == null)
            {
                string errorMessage = "Cannot connect to Provisioning Service.";
                if (Logging.IsEnabled) Logging.Error(this, errorMessage);
                ExceptionDispatchInfo.Capture(lastException).Throw();
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        private async Task<RegistrationOperationStatus> ProvisionOverWssUsingX509CertificateAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            Debug.Assert(message.Security is SecurityProviderX509);
            cancellationToken.ThrowIfCancellationRequested();

            X509Certificate2 clientCertificate =
                ((SecurityProviderX509)message.Security).GetAuthenticationCertificate();

            var tcs = new TaskCompletionSource<RegistrationOperationStatus>();

            UriBuilder uriBuilder = new UriBuilder(WsScheme, message.GlobalDeviceEndpoint, Port);
            Uri websocketUri = uriBuilder.Uri;

            // TODO properly dispose of the ws.
            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(WsMqttSubprotocol);
            websocket.Options.ClientCertificates.Add(clientCertificate);

            await websocket.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);

            // TODO: use ClientWebSocketChannel.
            var clientChannel = new ClientWebSocketChannel(null, websocket);
            clientChannel
                .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .Option(ChannelOption.MessageSizeEstimator, DefaultMessageSizeEstimator.Default)
                .Pipeline.AddLast(
                    new ReadTimeoutHandler(ReadTimeoutSeconds),
                    MqttEncoder.Instance,
                    new MqttDecoder(false, MaxMessageSize),
                    new ProvisioningChannelHandlerAdapter(message, tcs, cancellationToken));

            await s_eventLoopGroup.RegisterAsync(clientChannel).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
