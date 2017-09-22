// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The DPSRegistrationResult type returned when the X509 Certificate HSM mode is used.
    /// </summary>
    public class ProvisioningRegistrationResultX509Certificate : ProvisioningRegistrationResult
    {
        /// <summary>
        /// TODO: CertificateInformation (AliasCertificate?).
        /// In the current PoC this is empty.
        /// </summary>
        public ProvisioningX509CertificateInfo CertificateInfo { get; set; }

        /// <summary>
        /// The Enrollment Group Id.
        /// </summary>
        public string EnrollmentGroupId { get; set; }

        /// <summary>
        /// TODO: CertificateInformation (AliasCertificate?).
        /// In the current PoC this is empty.
        /// </summary>
        public ProvisioningX509CertificateInfo SigningCertificateInfo { get; set; }
    }
}
