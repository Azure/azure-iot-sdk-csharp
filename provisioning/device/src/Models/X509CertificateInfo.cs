// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

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
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; internal set; }

        /// <summary>
        /// The certificate SHA1 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string Sha1Thumbprint { get; internal set; }

        /// <summary>
        /// The certificate SHA256 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string Sha256Thumbprint { get; internal set; }

        /// <summary>
        /// The certificate issuer name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName")]
        public string IssuerName { get; internal set; }

        /// <summary>
        /// The certificate invalidity before date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTimeOffset? NotBeforeUtc { get; internal set; }

        /// <summary>
        /// The certificate invalidity after date in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTimeOffset? NotAfterUtc { get; internal set; }

        /// <summary>
        /// The certificate serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; internal set; }

        /// <summary>
        /// The certficiate version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int? Version { get; internal set; }
    }
}
