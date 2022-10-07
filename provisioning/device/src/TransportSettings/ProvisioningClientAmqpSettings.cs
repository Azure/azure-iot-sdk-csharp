// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Contains AMQP transport-specific settings for a provisioning device client.
    /// </summary>
    public sealed class ProvisioningClientAmqpSettings : ProvisioningClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol; defaults to TCP.</param>
        public ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol transportProtocol = ProvisioningClientTransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
        }

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
        /// The client web socket to use when communicating over web sockets.
        /// </summary>
        /// <remarks>
        /// This option is ignored for TCP connections.
        /// <para>
        /// If not provided, a client web socket instance will be created for you based on the other
        /// settings provided in this class. If provided, all other web socket level options set in this
        /// class will be ignored (WebSocketKeepAlive, proxy, and x509 certificates, for example).
        /// </para>
        /// </remarks>
        public ClientWebSocket ClientWebSocket { get; set; }

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        internal override ProvisioningClientTransportSettings Clone()
        {
            return new ProvisioningClientAmqpSettings(Protocol)
            {
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                IdleTimeout = IdleTimeout,
                ClientWebSocket = ClientWebSocket,
                WebSocketKeepAlive = WebSocketKeepAlive,
            };
        }
    }
}
