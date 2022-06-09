// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A common place to specify TLS information for the project, when the code must be explicit or a user requires an override.
    /// </summary>
    /// <remarks>
    /// As newer TLS versions come out, we should simply update them here and have all code that needs to
    /// know reference this class so they all get updated and we don't risk missing a line.
    /// </remarks>
    public class TlsVersions
    {
        /// <summary>
        /// A static instance of this class to be used by the Azure IoT .NET SDKs when opening connections
        /// </summary>
        public static readonly TlsVersions Instance = new TlsVersions();

        /// <summary>
        /// Internal constructor for testing. The SDK and users should use the static Instance property.
        /// </summary>
        internal TlsVersions()
        {
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
        public SslProtocols Preferred { get; private set; } = SslProtocols.None;

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