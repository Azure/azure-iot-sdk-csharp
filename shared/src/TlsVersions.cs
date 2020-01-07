using System.Security.Authentication;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A common place to specify TLS information for the project.
    /// </summary>
    /// <remarks>
    /// As newer TLS versions come out, we should simply update them here and have all code that needs to
    /// know reference this class so they all get updated and we don't risk missing a line.
    /// </remarks>
    public static class TlsVersions
    {
        /// <summary>
        /// The acceptable versions of TLS
        /// </summary>
        public const SslProtocols AcceptableVersions = SslProtocols.Tls12;

        /// <summary>
        /// Sets the acceptable versions of TLS for .NET framework 4.5.1.
        /// </summary>
        public static void SetLegacyAcceptableVersions()
        {
#if NET451
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
#endif
        }
    }
}
