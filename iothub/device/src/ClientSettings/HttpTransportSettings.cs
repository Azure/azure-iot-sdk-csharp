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
    /// Contains HTTP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class HttpTransportSettings : ITransportSettings
    {
        /// <inheritdoc/>
        public TransportProtocol Protocol { get; } = TransportProtocol.WebSocket;

        /// <inheritdoc/>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan DefaultReceiveTimeout { get; } = TimeSpan.FromMinutes(1);


        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; } = DefaultWebProxySettings.Instance;

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
