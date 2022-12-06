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
        protected internal X509CertificateInfo()
        {
        }

        /// <summary>
        /// The certificate subject name.
        /// </summary>
        [JsonProperty("subjectName")]
        public string SubjectName { get; protected internal set; }

        /// <summary>
        /// The certificate SHA1 thumbprint.
        /// </summary>
        [JsonProperty("sha1Thumbprint")]
        public string Sha1Thumbprint { get; protected internal set; }

        /// <summary>
        /// The certificate SHA256 thumbprint.
        /// </summary>
        [JsonProperty("sha256Thumbprint")]
        public string Sha256Thumbprint { get; protected internal set; }

        /// <summary>
        /// The certificate issuer name.
        /// </summary>
        [JsonProperty("issuerName")]
        public string IssuerName { get; protected internal set; }

        /// <summary>
        /// The certificate invalidity before date in UTC.
        /// </summary>
        [JsonProperty("notBeforeUtc")]
        public DateTimeOffset? NotBeforeUtc { get; protected internal set; }

        /// <summary>
        /// The certificate invalidity after date in UTC.
        /// </summary>
        [JsonProperty("notAfterUtc")]
        public DateTimeOffset? NotAfterUtc { get; protected internal set; }

        /// <summary>
        /// The certificate serial number.
        /// </summary>
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; protected internal set; }

        /// <summary>
        /// The certficiate version.
        /// </summary>
        [JsonProperty("version")]
        public int? Version { get; protected internal set; }
    }
}
