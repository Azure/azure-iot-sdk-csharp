// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// X509 certificate info.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    internal partial class X509CertificateInfo
    {
        /// <summary>
        /// Initializes a new instance of the X509CertificateInfo class.
        /// </summary>
        public X509CertificateInfo()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the X509CertificateInfo class.
        /// </summary>
        public X509CertificateInfo(
            string subjectName = default,
            string sha1Thumbprint = default,
            string sha256Thumbprint = default,
            string issuerName = default,
            DateTime? notBeforeUtc = default,
            DateTime? notAfterUtc = default,
            string serialNumber = default,
            int? version = default)
        {
            SubjectName = subjectName;
            Sha1Thumbprint = sha1Thumbprint;
            Sha256Thumbprint = sha256Thumbprint;
            IssuerName = issuerName;
            NotBeforeUtc = notBeforeUtc;
            NotAfterUtc = notAfterUtc;
            SerialNumber = serialNumber;
            Version = version;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// The certificate subject name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The certificate SHA1 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string Sha1Thumbprint { get; set; }

        /// <summary>
        /// The certificate SHA256 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string Sha256Thumbprint { get; set; }

        /// <summary>
        /// The certfificate issuer name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName")]
        public string IssuerName { get; set; }

        /// <summary>
        /// The certfificate invalidity before date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTime? NotBeforeUtc { get; set; }

        /// <summary>
        /// The certificate invalidity after date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTime? NotAfterUtc { get; set; }

        /// <summary>
        /// The certificate serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// The certficiate version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int? Version { get; set; }

    }
}
