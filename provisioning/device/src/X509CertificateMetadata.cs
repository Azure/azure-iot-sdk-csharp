// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Representation of a single X509 Certificate metadata.
    /// </summary>
    /// <example>
    /// The following JSON is an example of the X509 certificate info.
    /// <c>
    /// {
    ///     "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///     "sha1Thumbprint": "0000000000000000000000000000000000",
    ///     "sha256Thumbprint": "validEnrollmentGroupId",
    ///     "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///     "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///     "notAfterUtc": "2017-11-20T12:34:18Z",
    ///     "serialNumber": "000000000000000000",
    ///     "version": 3
    /// }
    /// </c>
    /// </example>
    public class X509CertificateMetadata
    {
        /// <summary>
        /// The distinguished name from the certificate.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SubjectName { get; internal set; }

        /// <summary>
        /// The SHA-1 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Sha1Thumbprint { get; internal set; }

        /// <summary>
        /// The SHA-256 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Sha256Thumbprint { get; internal set; }

        /// <summary>
        /// The distinguished name of the issuer of the certificate.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string IssuerName { get; internal set; }

        /// <summary>
        /// The date on which the certificate becomes valid.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime NotBeforeUtc { get; internal set; }

        /// <summary>
        /// The date on which the certificate is no longer valid.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime NotAfterUtc { get; internal set; }

        /// <summary>
        /// The X509 format version.
        /// </summary>
        [JsonProperty(PropertyName = "version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int SerialNumber { get; internal set; }
    }
}
