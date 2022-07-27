// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Base class used to define various transport-specific settings for IoT hub device and module clients.
    /// </summary>
    public abstract class IotHubClientTransportSettings
    {
        /// <summary>
        /// The configured transport protocol.
        /// </summary>
        public TransportProtocol Protocol { get; protected set; }

        /// <summary>
        /// The client certificate to use for authenticating.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// The time to wait for a receive operation.
        /// </summary>
        public TimeSpan DefaultReceiveTimeout { get; protected set; }

        /// <summary>
        /// The web proxy that will be used to connect to IoT hub using a web socket connection for AMQP, MQTT, or when using the
        /// HTTP protocol.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// This setting will only be used when the client connects over web sockets or HTTPS.
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from
        /// <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost
        /// on port 8888.
        /// <c>
        /// static DeviceClient GetClientWithProxy()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         // Specify the WebProxy to be used for the web socket connection
        ///         var transportSettings = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only)
        ///         {
        ///             Proxy = new WebProxy(proxyHost, proxyPort)
        ///         };
        ///         return DeviceClient.CreateFromConnectionString("a connection string", new TransportSettings[] { transportSettings });
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </c>
        /// </example>
        public IWebProxy Proxy { get; set; }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }

        /// <summary>
        /// The acceptable versions of TLS to use when the SDK must be explicit.
        /// </summary>
        public SslProtocols MinimumTlsVersions { get; private set; } = SslProtocols.Tls12;

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols PreferredTlsVersions { get; private set; } = SslProtocols.None;

#pragma warning disable CA5397 // Do not use deprecated SslProtocols values
        private const SslProtocols AllowedProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        private const SslProtocols PreferredProtocol = SslProtocols.Tls12;

        /// <summary>
        /// To enable certificate revocation check. Default to be false.
        /// </summary>
        public bool CertificateRevocationCheck { get; set; }

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
                PreferredTlsVersions = SslProtocols.None;
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

            MinimumTlsVersions = PreferredTlsVersions = protocols;
        }

#pragma warning restore CA5397 // Do not use deprecated SslProtocols values
    }
}
