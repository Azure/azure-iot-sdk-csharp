// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// X509 certificate info.
    /// </summary>
    public class X509CertificateInfo
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        internal X509CertificateInfo()
        {
        }

        /// <summary>
        /// The certificate subject name.
        /// </summary>
        [JsonPropertyName("subjectName")]
        public string SubjectName { get; internal set; }

        /// <summary>
        /// The certificate SHA1 thumbprint.
        /// </summary>
        [JsonPropertyName("sha1Thumbprint")]
        public string Sha1Thumbprint { get; internal set; }

        /// <summary>
        /// The certificate SHA256 thumbprint.
        /// </summary>
        [JsonPropertyName("sha256Thumbprint")]
        public string Sha256Thumbprint { get; internal set; }

        /// <summary>
        /// The certificate issuer name.
        /// </summary>
        [JsonPropertyName("issuerName")]
        public string IssuerName { get; internal set; }

        /// <summary>
        /// The certificate invalidity before date in UTC.
        /// </summary>
        [JsonPropertyName("notBeforeUtc")]
        public DateTimeOffset? NotBeforeUtc { get; internal set; }

        /// <summary>
        /// The certificate invalidity after date in UTC.
        /// </summary>
        [JsonPropertyName("notAfterUtc")]
        public DateTimeOffset? NotAfterUtc { get; internal set; }

        /// <summary>
        /// The certificate serial number.
        /// </summary>
        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; internal set; }

        /// <summary>
        /// The certficiate version.
        /// </summary>
        [JsonPropertyName("version")]
        public int? Version { get; internal set; }
    }
}
