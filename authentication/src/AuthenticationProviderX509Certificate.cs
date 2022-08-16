// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Authentication
{
    /// <summary>
    /// The device authentication for using an X509 certificate object.
    /// </summary>
    public class AuthenticationProviderX509Certificate : AuthenticationProviderX509
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2Collection _certificateChain;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="clientCertificate">
        /// The client certificate used for authentication. The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </param>
        /// <param name="certificateChain">
        /// The certificate chain leading to the root certificate uploaded to the device provisioning service.
        /// </param>
        public AuthenticationProviderX509Certificate(
            X509Certificate2 clientCertificate,
            X509Certificate2Collection certificateChain = null)
        {
            _clientCertificate = clientCertificate;
            _certificateChain = certificateChain;
        }

        /// <summary>
        /// Gets the certificate trust chain that will end in the Trusted Root installed on the server side.
        /// </summary>
        /// <returns>The certificate chain.</returns>
        public override X509Certificate2 GetAuthenticationCertificate()
        {
            return _clientCertificate;
        }

        /// <summary>
        /// Gets the certificate used for TLS device authentication.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public override X509Certificate2Collection GetAuthenticationCertificateChain()
        {
            return _certificateChain;
        }
    }
}
