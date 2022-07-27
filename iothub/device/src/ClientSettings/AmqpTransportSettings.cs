// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains AMQP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class AmqpTransportSettings : ITransportSettings
    {
        private TimeSpan _operationTimeout = DefaultOperationTimeout;

        /// <summary>
        /// The default operation timeout.
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The default idle timeout.
        /// </summary>
        public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// The default pre-fetch count.
        /// </summary>
        public const uint DefaultPrefetchCount = 50;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol; defaults to TCP.</param>
        public AmqpTransportSettings(TransportProtocol transportProtocol = TransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
            if (Protocol == TransportProtocol.WebSocket)
            {
                Proxy = DefaultWebProxySettings.Instance;
            }
        }

        /// <inheritdoc/>
        public TransportProtocol Protocol { get; }

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections
        /// </summary>
        internal string AuthenticationChain { get; set; }

        /// <summary>
        /// To enable certificate revocation check. Default to be false.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Cannot change public property in public facing classes.")]
        public bool CertificateRevocationCheck { get; set; }

        /// <summary>
        /// Specify client-side heartbeat interval.
        /// The interval, that the client establishes with the service, for sending keep alive pings.
        /// The default value is 2 minutes.
        /// </summary>
        /// <remarks>
        /// The client will consider the connection as disconnected if the keep alive ping fails.
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </remarks>
        public TimeSpan IdleTimeout { get; set; } = DefaultIdleTimeout;

        /// <summary>
        /// The time to wait for any operation to complete. The default is 1 minute.
        /// </summary>
        public TimeSpan OperationTimeout
        {
            get => _operationTimeout;
            set => _operationTimeout = value > TimeSpan.Zero
                ? value
                : throw new ArgumentOutOfRangeException(nameof(OperationTimeout), "Must be greather than zero.");
        }

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        /// <summary>
        /// The pre-fetch count
        /// </summary>
        public uint PrefetchCount { get; set; } = DefaultPrefetchCount;

        /// <inheritdoc/>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to IoTHub and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// The connection pool settings for AMQP
        /// </summary>
        public AmqpConnectionPoolSettings ConnectionPoolSettings { get; set; }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        public TimeSpan DefaultReceiveTimeout => DefaultOperationTimeout;

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }

        /// <summary>
        /// The acceptable versions of TLS to use when the SDK must be explicit.
        /// </summary>
        public SslProtocols MinimumTlsVersions { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols Preferred { get; set; } = SslProtocols.None;

#pragma warning disable CA5397 // Do not use deprecated SslProtocols values
        private const SslProtocols AllowedProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        private const SslProtocols PreferredProtocol = SslProtocols.Tls12;

        /// <summary>
        /// Sets the minimum acceptable versions of TLS.
        /// </summary>
        /// <remarks>
        /// Will ignore a version less than TLS 1.0.
        ///
        /// Affects:
        /// 1. MinimumTlsVersions property
        /// 2. Preferred property
        /// 3. For .NET framework 4.5.1 over HTTPS or websocket, as it does not offer a "SystemDefault" option, the version must be explicit.
        /// </remarks>
        public void SetMinimumTlsVersions(SslProtocols protocols = SslProtocols.None)
        {
            // sanitize to only those that are allowed
            protocols &= AllowedProtocols;

            if (protocols == SslProtocols.None)
            {
                Preferred = SslProtocols.None;
                MinimumTlsVersions = PreferredProtocol;
                return;
            }

            // ensure the preferred TLS version is included
            if (((protocols & SslProtocols.Tls) != 0
                    || (protocols & SslProtocols.Tls11) != 0)
                && (protocols & PreferredProtocol) == 0)
            {
                protocols ^= PreferredProtocol;
            }

            MinimumTlsVersions = Preferred = protocols;
        }

#pragma warning restore CA5397 // Do not use deprecated SslProtocols values
    }
}
