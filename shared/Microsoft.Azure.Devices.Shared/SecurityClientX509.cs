// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for X509 authentication using a certificate object.
    /// </summary>
    public class SecurityClientX509 : SecurityClientHsmX509
    {
        private X509Certificate2 _clientCertificate;
        private X509Certificate2Collection _certificateChain;

        /// <summary>
        /// Initializes a new instance of the SecurityClientX509 class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate used for authentication.</param>
        /// <param name="certificateChain">The certificate chain leading to the root certificate uploaded to the Provisioning service.</param>
        public SecurityClientX509(
            X509Certificate2 clientCertificate,
            X509Certificate2Collection certificateChain = null)
        {
            _clientCertificate = clientCertificate;
            _certificateChain = certificateChain;
        }

        public override X509Certificate2 GetAuthenticationCertificate()
        {
            return _clientCertificate;
        }

        public override X509Certificate2Collection GetAuthenticationCertificateChain()
        {
            return _certificateChain;
        }

        protected override void Dispose(bool disposing) { }
    }
}
