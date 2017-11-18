// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

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
    /// <code>
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
    /// <seealso cref="https://www.trustedcomputinggroup.org/wp-content/uploads/Device-Identifier-Composition-Engine-Rev69_Public-Review.pdf">Device Identifier Composition Engine (DICE) spec</seealso>
    /// <seealso cref="https://www.microsoft.com/en-us/research/publication/riot-a-foundation-for-trust-in-the-internet-of-things">RIoT – A Foundation for Trust in the Internet of Things</seealso>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollmentgroup">Device Enrollment Group</seealso>
    public class X509CertificateInfo
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the <see cref="X509CertificateInfo"/> using the provided set of info in the JSON.
        /// This method will validate each parameter and the object consistence.
        /// </remarks>
        /// 
        /// <param name="subjectName">the distinguished name from the certificate.</param>
        /// <param name="sha1Thumbprint">the SHA-1 hash value of the certificate as a hexadecimal string.</param>
        /// <param name="sha256Thumbprint">the SHA-256 hash value of the certificate as a hexadecimal string.</param>
        /// <param name="issuerName">the issuer distinguished name.</param>
        /// <param name="notBeforeUtc">the date on which the certificate becomes valid.</param>
        /// <param name="notAfterUtc">the date on which the certificate is no longer valid.</param>
        /// <param name="serialNumber">the serial number.</param>
        /// <param name="version">the x509 format version</param>
        /// <exception cref="ProvisioningServiceClientException">if one of the provided info is invalid.</exception>
        [JsonConstructor]
        private X509CertificateInfo(
            string subjectName, string sha1Thumbprint, string sha256Thumbprint, 
            string issuerName, DateTime? notBeforeUtc, DateTime? notAfterUtc, string serialNumber, int? version)
        {
            /* SRS_X509_CERTIFICATE_INFO_21_001: [The X509CertificateInfo shall provide means to deserialization.] */
            try
            {
                ParserUtils.EnsureUTF8String(subjectName);
                ParserUtils.EnsureUTF8String(sha1Thumbprint);
                ParserUtils.EnsureUTF8String(sha256Thumbprint);
                ParserUtils.EnsureUTF8String(issuerName);
                ParserUtils.EnsureUTF8String(serialNumber);
            }
            catch (ArgumentException e)
            {
                throw new ProvisioningServiceClientException(e);
            }

            if ((notBeforeUtc == null) || (notAfterUtc == null) || (version == null))
            {
                throw new ProvisioningServiceClientException("DateTime cannot be null");
            }

            SubjectName = subjectName;
            SHA1Thumbprint = sha1Thumbprint;
            SHA256Thumbprint = sha256Thumbprint;
            IssuerName = issuerName;
            NotBeforeUtc = (DateTime)notBeforeUtc;
            NotAfterUtc = (DateTime)notAfterUtc;
            SerialNumber = serialNumber;
            Version = (int)version;
        }

        /// <summary>
        /// Distinguished name from the certificate.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; private set; }

        /// <summary>
        /// SHA-1 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha1Thumbprint")]
        public string SHA1Thumbprint { get; private set; }

        /// <summary>
        /// SHA-256 hash value of the certificate as a hexadecimal string.
        /// </summary>
        [JsonProperty(PropertyName = "sha256Thumbprint")]
        public string SHA256Thumbprint { get; private set; }

        /// <summary>
        /// Issuer distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "issuerName")]
        public string IssuerName { get; private set; }

        /// <summary>
        /// The date on which the certificate becomes valid.
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc")]
        public DateTime NotBeforeUtc { get; private set; }

        /// <summary>
        /// The date on which the certificate is no longer valid.
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc")]
        public DateTime NotAfterUtc { get; private set; }

        /// <summary>
        /// The serial number.
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; private set; }

        /// <summary>
        /// The X509 format version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; private set; }
    }
}
