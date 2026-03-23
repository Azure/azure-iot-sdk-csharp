// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text.Json;
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
        /// 
        /// </summary>
        /// <param name="subjectName"></param>
        /// <param name="sha1Thumbprint"></param>
        /// <param name="sha256Thumbprint"></param>
        /// <param name="issuerName"></param>
        /// <param name="notBeforeUtc"></param>
        /// <param name="notAfterUtc"></param>
        /// <param name="serialNumber"></param>
        /// <param name="version"></param>
        /// <exception cref="ProvisioningServiceException"></exception>
        [JsonConstructor]
        public X509CertificateInfo(
            string subjectName,
            string sha1Thumbprint,
            string sha256Thumbprint,
            string issuerName,
            DateTimeOffset? notBeforeUtc,
            DateTimeOffset? notAfterUtc,
            string serialNumber,
            int? version)
        {
            if (notBeforeUtc == null
                || notAfterUtc == null
                || version == null)
            {
                throw new ProvisioningServiceException("DateTime cannot be null", HttpStatusCode.BadRequest);
            }

            SubjectName = subjectName;
            Sha1Thumbprint = sha1Thumbprint;
            Sha256Thumbprint = sha256Thumbprint;
            IssuerName = issuerName;
            NotBeforeUtc = (DateTimeOffset)notBeforeUtc;
            NotAfterUtc = (DateTimeOffset)notAfterUtc;
            SerialNumber = serialNumber;
            Version = (int)version;
        }

        /// <summary>
        /// For unit testing.
        /// </summary>
        protected internal X509CertificateInfo()
        {
        }

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
