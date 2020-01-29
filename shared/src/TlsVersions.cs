using System.Net;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A common place to specify TLS information for the project, when the code must be explicit or a user requires an override.
    /// </summary>
    /// <remarks>
    /// As newer TLS versions come out, we should simply update them here and have all code that needs to
    /// know reference this class so they all get updated and we don't risk missing a line.
    /// </remarks>
    public static class TlsVersions
    {
        /// <summary>
        /// The acceptable versions of TLS to use when the SDK must be explicit.
        /// </summary>
        public static SslProtocols MinimumTlsVersions { get; private set; } = SslProtocols.Tls12;

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public static SslProtocols Preferred { get; private set; } = SslProtocols.None;

#if NET451
        private static SecurityProtocolType _net451Protocol = (SecurityProtocolType)_preferredProtocol;
#endif

        private const SslProtocols _allowedProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        private const SslProtocols _preferredProtocol = SslProtocols.Tls12;

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
        public static void SetMinimumTlsVersions(SslProtocols protocols = SslProtocols.None)
        {
            // sanitize to only those that are allowed
            protocols &= _allowedProtocols;

            if (protocols == SslProtocols.None)
            {
                Preferred = SslProtocols.None;
                MinimumTlsVersions = _preferredProtocol;
#if NET451
                _net451Protocol = (SecurityProtocolType)_preferredProtocol;
#endif
                return;
            }

            // ensure the prefered TLS version is included
            if (((protocols & SslProtocols.Tls) != 0
                    || (protocols & SslProtocols.Tls11) != 0)
                && (protocols & _preferredProtocol) == 0)
            {
                protocols ^= _preferredProtocol;
            }

            MinimumTlsVersions = Preferred = protocols;
#if NET451
            // this works because the different enums have the same numeric values
            _net451Protocol = (SecurityProtocolType)protocols;
#endif
        }

        /// <summary>
        /// Sets the acceptable versions of TLS over HTTPS or websocket for .NET framework 4.5.1, as it does not offer a "SystemDefault" option. No-op for other .NET versions.
        /// </summary>
        public static void SetLegacyAcceptableVersions()
        {
#if NET451
            ServicePointManager.SecurityProtocol = _net451Protocol;
#endif
        }
    }
}