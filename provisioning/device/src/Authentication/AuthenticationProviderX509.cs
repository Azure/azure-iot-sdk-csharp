// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The device authentication for using an X509 certificate object.
    /// </summary>
    public class AuthenticationProviderX509 : AuthenticationProvider
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2Collection _certificateChain;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// Ensure that you dispose any supplied <see cref="X509Certificate2"/> after you are done using it to ensure there are no memory leaks.
        /// </remarks>
        /// <param name="clientCertificate">
        /// The client certificate used for authentication. The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </param>
        /// <param name="certificateChain">
        /// The certificate chain leading to the root certificate uploaded to the device provisioning service.
        /// </param>
        public AuthenticationProviderX509(
            X509Certificate2 clientCertificate,
            X509Certificate2Collection certificateChain = null)
        {
            _clientCertificate = clientCertificate;
            _certificateChain = certificateChain;
        }

        /// <inheritdoc/>
        public override string GetRegistrationId()
        {
            X509Certificate2 cert = GetAuthenticationCertificate();
            return cert.GetNameInfo(X509NameType.DnsName, false);
        }

        /// <summary>
        /// Gets the certificate trust chain that will end in the Trusted Root installed on the server side.
        /// </summary>
        /// <returns>The certificate chain.</returns>
        public X509Certificate2 GetAuthenticationCertificate()
        {
            return _clientCertificate;
        }

        /// <summary>
        /// Gets the certificate used for TLS device authentication.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public X509Certificate2Collection GetAuthenticationCertificateChain()
        {
            return _certificateChain;
        }
    }
}
