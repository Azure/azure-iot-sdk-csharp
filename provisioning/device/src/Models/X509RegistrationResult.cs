// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// X509 registration result.
    /// </summary>
    public class X509RegistrationResult
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        protected internal X509RegistrationResult()
        {
        }

        /// <summary>
        /// Information about the X509 certificate.
        /// </summary>
        [JsonProperty("certificateInfo")]
        public X509CertificateInfo CertificateInfo { get; protected internal set; }

        /// <summary>
        /// The device provisioning service enrollment group Id.
        /// </summary>
        [JsonProperty("enrollmentGroupId")]
        public string EnrollmentGroupId { get; protected internal set; }

        /// <summary>
        /// Signing information about the certificate.
        /// </summary>
        [JsonProperty("signingCertificateInfo")]
        public X509CertificateInfo SigningCertificateInfo { get; protected internal set; }

    }
}
