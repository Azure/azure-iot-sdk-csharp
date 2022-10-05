// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
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
        /// An instance of client web socket to be used when transport protocol is set to web socket.
        /// </summary>
        /// <remarks>
        /// If not provided, an instance will be created from provided values for Proxy and Certificate.
        /// </remarks>
        public ClientWebSocket ClientWebSocket { get; set; }

        internal override ProvisioningClientTransportSettings Clone()
        {
            return new ProvisioningClientAmqpSettings(Protocol)
            {
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                IdleTimeout = IdleTimeout,
            };
        }
    }
}
