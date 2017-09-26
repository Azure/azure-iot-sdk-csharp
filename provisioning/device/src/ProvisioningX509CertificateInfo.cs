// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Certificate information returned by the Device Provisioning Service.
    /// </summary>
    public class ProvisioningX509CertificateInfo
    {
        /// <summary>
        /// Certificate Subject Name.
        /// </summary>
        public string SubjectName { get; private set; }

        /// <summary>
        /// SHA1 Thumbprint of the certificate.
        /// </summary>
        public string Sha1Thumbprint { get; private set; }

        /// <summary>
        /// SHA256 Thumbprint of the certificate.
        /// </summary>
        public string Sha256Thumbprint { get; private set; }

        /// <summary>
        /// Certificate issuer name.
        /// </summary>
        public string IssuerName { get; private set; }

        /// <summary>
        /// Certificate validity start time.
        /// </summary>
        public System.DateTime? NotBeforeUtc { get; private set; }

        /// <summary>
        /// Certificate expiration.
        /// </summary>
        public System.DateTime? NotAfterUtc { get; private set; }

        /// <summary>
        /// Certificate serial number.
        /// </summary>
        public string SerialNumber { get; private set; }

        /// <summary>
        /// Certificate version.
        /// </summary>
        public int? Version { get; private set; }
    }
}
