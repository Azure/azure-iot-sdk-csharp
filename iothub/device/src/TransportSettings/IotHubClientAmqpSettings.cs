// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net.Security;
using System.Net.WebSockets;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains AMQP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class IotHubClientAmqpSettings : IotHubClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of this class with a default protocol of TCP.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol.</param>
        public IotHubClientAmqpSettings(IotHubClientTransportProtocol transportProtocol = IotHubClientTransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
        }

        /// <summary>
        /// The interval that the client establishes with the service for sending keep-alive pings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is 2 minutes.
        /// </para>
        /// <para>
        /// The client will consider the connection as disconnected if the keep-alive ping fails.
        /// </para>
        /// <para>
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </para>
        /// </remarks>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        /// <summary>
        /// The pre-fetch count.
        /// </summary>
        /// <remarks>
        /// This influences how much work the device can handle at a time and affects parallelism of messages.
        /// If the pre-fetch count is only 1 the AMQP transport library will get one message and will not get another,
        /// even if some are available, until the received message processing is finished.
        /// <para>
        /// With a default of 50, up to 50 messages (e.g., twin property updates, C2D messages, direct method calls)
        /// will be delivered to the client app at a time.
        /// </para>
        /// </remarks>
        public uint PrefetchCount { get; set; } = 50;

        /// <summary>
        /// A callback for remote certificate validation.
        /// </summary>
        /// <remarks>
        /// If incorrectly implemented, your device may fail to connect to IoT hub and/or be open to security vulnerabilities.
        /// <para>
        /// This feature is only applicable for AMQP over TCP. AMQP web socket communication does not support this feature.
        /// For users who want this support over AMQP websocket, you must instead provide a <see cref="ClientWebSocket"/>
        /// instance with the desired callback and other websocket options (e.g., proxy, keep-alive, etc.) set.
        /// </para>
        /// </remarks>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// An instance of client web socket to be used when transport protocol is set to web socket.
        /// </summary>
        /// <remarks>
        /// If not provided, an instance will be created from provided websocket options (eg. proxy, keep-alive etc.)
        /// </remarks>
        public ClientWebSocket ClientWebSocket { get; set; }

        /// <summary>
        /// If using pooling, specify connection pool settings.
        /// </summary>
        public AmqpConnectionPoolSettings ConnectionPoolSettings { get; set; }

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string AuthenticationChain { get; set; }

        internal override IotHubClientTransportSettings Clone()
        {
            return new IotHubClientAmqpSettings(Protocol)
            {
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                CertificateRevocationCheck = CertificateRevocationCheck,
                AuthenticationChain = AuthenticationChain,
                IdleTimeout = IdleTimeout,
                WebSocketKeepAlive = WebSocketKeepAlive,
                PrefetchCount = PrefetchCount,
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                ConnectionPoolSettings = ConnectionPoolSettings,
                ClientWebSocket = ClientWebSocket,
            };
        }
    }
}
