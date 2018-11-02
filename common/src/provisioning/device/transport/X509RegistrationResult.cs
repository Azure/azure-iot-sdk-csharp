// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// X509 registration result.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    internal partial class X509RegistrationResult
    {
        /// <summary>
        /// Initializes a new instance of the X509RegistrationResult class.
        /// </summary>
        public X509RegistrationResult()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the X509RegistrationResult class.
        /// </summary>
        public X509RegistrationResult(
            X509CertificateInfo certificateInfo = default(X509CertificateInfo), 
            string enrollmentGroupId = default(string), 
            X509CertificateInfo signingCertificateInfo = default(X509CertificateInfo))
        {
            CertificateInfo = certificateInfo;
            EnrollmentGroupId = enrollmentGroupId;
            SigningCertificateInfo = signingCertificateInfo;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "certificateInfo")]
        public X509CertificateInfo CertificateInfo { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "enrollmentGroupId")]
        public string EnrollmentGroupId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "signingCertificateInfo")]
        public X509CertificateInfo SigningCertificateInfo { get; set; }

    }
}
