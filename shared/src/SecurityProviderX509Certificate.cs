// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for X509 authentication using a certificate object.
    /// </summary>
    public class SecurityProviderX509Certificate : SecurityProviderX509
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2Collection _certificateChain;

        /// <summary>
        /// Initializes a new instance of the SecurityProviderX509Certificate class.
        /// </summary>
        /// <param name="clientCertificate">
        /// The client certificate used for authentication. The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </param>
        /// <param name="certificateChain">
        /// The certificate chain leading to the root certificate uploaded to the device provisioning service.
        /// </param>
        public SecurityProviderX509Certificate(
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

        /// <summary>
        /// Releases the unmanaged resources used by the SecurityProviderX509Certificate and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected override void Dispose(bool disposing) { }
    }
}
