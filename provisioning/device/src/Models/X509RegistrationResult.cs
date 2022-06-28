// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// X509 registration result.
    /// </summary>
    internal class X509RegistrationResult
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public X509RegistrationResult(
            X509CertificateInfo certificateInfo = default,
            string enrollmentGroupId = default,
            X509CertificateInfo signingCertificateInfo = default)
        {
            CertificateInfo = certificateInfo;
            EnrollmentGroupId = enrollmentGroupId;
            SigningCertificateInfo = signingCertificateInfo;
        }

        /// <summary>
        /// Information about the X509 certificate.
        /// </summary>
        [JsonProperty(PropertyName = "certificateInfo")]
        public X509CertificateInfo CertificateInfo { get; }

        /// <summary>
        /// The device provisioning service enrollment group Id.
        /// </summary>
        [JsonProperty(PropertyName = "enrollmentGroupId")]
        public string EnrollmentGroupId { get; }

        /// <summary>
        /// Signing information about the certificate.
        /// </summary>
        [JsonProperty(PropertyName = "signingCertificateInfo")]
        public X509CertificateInfo SigningCertificateInfo { get; }

    }
}
