// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single X509 Certificate Info and their accessors for the Device Provisioning Service.
    /// </summary>
    /// <remarks>
    /// User receive this info from the provisioning service as result of X509 operations.
    /// </remarks>
    public class X509CertificateInfo
    {
        /// <summary>
        /// Distinguished name from the certificate.
        /// </summary>
        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// SHA-1 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonPropertyName("sha1Thumbprint")]
        public string Sha1Thumbprint { get; set; }

        /// <summary>
        /// SHA-256 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonPropertyName("sha256Thumbprint")]
        public string Sha256Thumbprint { get; set; }

        /// <summary>
        /// Issuer distinguished name.
        /// </summary>
        [JsonPropertyName("issuerName")]
        public string IssuerName { get; set; }

        /// <summary>
        /// The date on which the certificate becomes valid.
        /// </summary>
        [JsonPropertyName("notBeforeUtc")]
        public DateTimeOffset NotBeforeUtc { get; set; }

        /// <summary>
        /// The date on which the certificate is no longer valid.
        /// </summary>
        [JsonPropertyName("notAfterUtc")]
        public DateTimeOffset NotAfterUtc { get; set; }

        /// <summary>
        /// The serial number.
        /// </summary>
        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// The X509 format version.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}
