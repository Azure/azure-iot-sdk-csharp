// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// X509 certificate info.
    /// </summary>
    internal class X509CertificateInfo
    {
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
        }

        /// <summary>
        /// The certificate subject name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; }

        /// <summary>
        /// The certificate SHA1 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string Sha1Thumbprint { get; }

        /// <summary>
        /// The certificate SHA256 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string Sha256Thumbprint { get; }

        /// <summary>
        /// The certificate issuer name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName")]
        public string IssuerName { get; }

        /// <summary>
        /// The certificate invalidity before date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTime? NotBeforeUtc { get; }

        /// <summary>
        /// The certificate invalidity after date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTime? NotAfterUtc { get; }

        /// <summary>
        /// The certificate serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; }

        /// <summary>
        /// The certficiate version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int? Version { get; }
    }
}
