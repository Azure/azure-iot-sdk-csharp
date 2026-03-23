// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// X509 registration result.
    /// </summary>
    public class X509RegistrationResult
    {
        /// <summary>
        /// Information about the X509 certificate.
        /// </summary>
        [JsonPropertyName("certificateInfo")]
        public X509CertificateInfo CertificateInfo { get; set; }

        /// <summary>
        /// The device provisioning service enrollment group Id.
        /// </summary>
        [JsonPropertyName("enrollmentGroupId")]
        public string EnrollmentGroupId { get; set; }

        /// <summary>
        /// Signing information about the certificate.
        /// </summary>
        [JsonPropertyName("signingCertificateInfo")]
        public X509CertificateInfo SigningCertificateInfo { get; set; }

    }
}
