// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Contains X509 certificate properties and their accessors.
    /// </summary>
    public class X509CertificateInfo
    {
        /// <summary>
        /// Distinguished name from the certificate.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// SHA-1 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string SHA1Thumbprint { get; set; }

        /// <summary>
        /// SHA-256 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string SHA256Thumbprint { get; set; }

        /// <summary>
        /// Issuer distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string IssuerName { get; set; }

        /// <summary>
        /// The date on which the certificate becomes valid.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTime NotBeforeUtc { get; set; }

        /// <summary>
        /// The date on which the certificate is no longer valid.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTime NotAfterUtc { get; set; }

        /// <summary>
        /// The serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// The X509 format version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }
    }
}
