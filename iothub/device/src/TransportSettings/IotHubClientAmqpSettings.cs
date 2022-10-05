// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol; defaults to TCP.</param>
        public IotHubClientAmqpSettings(IotHubClientTransportProtocol transportProtocol = IotHubClientTransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
        }

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections
        /// </summary>
        internal string AuthenticationChain { get; set; }

        /// <summary>
        /// Specify client-side heartbeat interval.
        /// The interval, that the client establishes with the service, for sending keep alive pings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is 2 minutes.
        /// </para>
        /// <para>
        /// The client will consider the connection as disconnected if the keep alive ping fails.
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
        public uint PrefetchCount { get; set; } = 50;

        /// <summary>
        /// A callback for remote certificate validation.
        /// </summary>
        /// <remarks>
        /// If incorrectly implemented, your device may fail to connect to IoT hub and/or be open to security vulnerabilities.
        /// <para>
        /// This feature is only applicable for HTTP, MQTT over TCP, MQTT over web socket, AMQP
        /// over TCP. AMQP web socket communication does not support this feature. For users who want
        /// this support over AMQP websocket, you must instead provide a <see cref="ClientWebSocket"/>
        /// instance with the desired callback set.
        /// </para>
        /// </remarks>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// An instance of client web socket to be used when transport protocol is set to web socket.
        /// </summary>
        /// <remarks>
        /// If not provided, an instance will be created from provided values for
        /// <see cref="IotHubClientTransportSettings.Proxy"/> and <see cref="IotHubConnectionCredentials.Certificate"/>.
        /// </remarks>
        public ClientWebSocket ClientWebSocket { get; set; }

        /// <summary>
        /// If using pooling, specify connection pool settings.
        /// </summary>
        public AmqpConnectionPoolSettings ConnectionPoolSettings { get; set; }

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
