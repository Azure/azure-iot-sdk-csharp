// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single X509 Certificate Info and their accessors for the Device Provisioning Service.
    /// </summary>
    /// <remarks>
    /// User receive this info from the provisioning service as result of X509 operations.
    /// </remarks>
    /// <example>
    /// This info contains a set of parameters, The following JSON is an example of the X509 certificate info.
    /// <code language="json">
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
    /// </code>
    /// </example>
    public class X509CertificateInfo
    {
        [JsonConstructor]
        private X509CertificateInfo(
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
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; protected private set; }

        /// <summary>
        /// SHA-1 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string Sha1Thumbprint { get; protected private set; }

        /// <summary>
        /// SHA-256 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string Sha256Thumbprint { get; protected private set; }

        /// <summary>
        /// Issuer distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName")]
        public string IssuerName { get; protected private set; }

        /// <summary>
        /// The date on which the certificate becomes valid.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTimeOffset NotBeforeUtc { get; protected private set; }

        /// <summary>
        /// The date on which the certificate is no longer valid.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTimeOffset NotAfterUtc { get; protected private set; }

        /// <summary>
        /// The serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; protected private set; }

        /// <summary>
        /// The X509 format version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; protected private set; }
    }
}
