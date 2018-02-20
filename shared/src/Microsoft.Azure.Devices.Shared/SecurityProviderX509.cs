// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Provider interface for X.509-based Hardware Security Modules.
    /// </summary>
    public abstract class SecurityProviderX509 : SecurityProvider
    {
        /// <summary>
        /// Returns the RegistrationID.
        /// </summary>
        /// <returns>The RegistrationID.</returns>
        public override string GetRegistrationID()
        {
            X509Certificate2 cert = GetAuthenticationCertificate();
            return cert.GetNameInfo(X509NameType.DnsName, false);
        }

        /// <summary>
        /// Gets the certificate trust chain that will end in the Trusted Root installed on the server side.
        /// </summary>
        /// <returns>The certificate chain.</returns>
        public abstract X509Certificate2Collection GetAuthenticationCertificateChain();

        /// <summary>
        /// Gets the certificate used for TLS device authentication.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public abstract X509Certificate2 GetAuthenticationCertificate();
    }
}
