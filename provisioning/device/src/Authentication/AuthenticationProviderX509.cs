// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The device authentication for using an X509 certificate object.
    /// </summary>
    public class AuthenticationProviderX509 : AuthenticationProvider
    {
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
            ClientCertificate = clientCertificate
                ?? throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.", nameof(clientCertificate));

            CertificateChain = certificateChain;
        }

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected internal AuthenticationProviderX509()
        { }

        /// <summary>
        /// The client certificate used for TLS device authentication.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; }

        /// <summary>
        /// The certificate trust chain that will end in the Trusted Root installed on the server side.
        /// </summary>
        public X509Certificate2Collection CertificateChain { get; }

        /// <inheritdoc/>
        public override string GetRegistrationId()
        {
            return ClientCertificate.GetNameInfo(X509NameType.DnsName, false);
        }
    }
}
