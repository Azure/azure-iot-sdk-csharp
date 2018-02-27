// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Primary and Secondary Certificates.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate. It can receive primary and secondary
    /// certificate, but only the primary is mandatory.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <code>
    ///  {
    ///      "primary": {
    ///          "certificate": "-----BEGIN CERTIFICATE-----\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "-----END CERTIFICATE-----\n";
    ///      },
    ///      "secondary": {
    ///          "certificate": "-----BEGIN CERTIFICATE-----\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "-----END CERTIFICATE-----\n";
    ///      }
    ///  }
    /// </code>
    ///
    /// After send an X509 certificate with success, the provisioning service will return the <see cref="X509CertificateInfo"/>
    /// for both primary and secondary certificate. User can get these info from this class, and once again, only
    /// the primary info is mandatory. The following JSON is an example what info the provisioning service will
    /// return for X509.
    /// <code>
    ///  {
    ///      "primary": {
    ///          "info": {
    ///               "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "sha1Thumbprint": "0000000000000000000000000000000000",
    ///               "sha256Thumbprint": "validEnrollmentGroupId",
    ///               "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///               "notAfterUtc": "2017-11-20T12:34:18Z",
    ///               "serialNumber": "000000000000000000",
    ///               "version": 3
    ///           }
    ///      },
    ///      "secondary": {
    ///          "info": {
    ///               "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "sha1Thumbprint": "0000000000000000000000000000000000",
    ///               "sha256Thumbprint": "validEnrollmentGroupId",
    ///               "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///               "notAfterUtc": "2017-11-20T12:34:18Z",
    ///               "serialNumber": "000000000000000000",
    ///               "version": 3
    ///           }
    ///      }
    ///  }
    /// </code>
    /// </example>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    public class X509Certificates 
    {

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the <code>X509Certificates</code> using the provided set of <see cref="X509Certificate2"/>.
        /// </remarks>
        /// <param name="primary">the <code>X509Certificate2</code> with the provisioning certificate and info. It cannot be <code>null</code>.</param>
        /// <param name="secondary">the <code>X509Certificate2</code> with the provisioning certificate and info. It can be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if the provided certificate is <code>null</code>.</exception>
        /// <exception cref="CryptographicException">if the provided certificate is invalid.</exception>
        internal X509Certificates(X509Certificate2 primary, X509Certificate2 secondary = null)
        {
            /* SRS_X509_CERTIFICATES_21_001: [The constructor shall store the provided primary and secondary certificates as X509CertificateWithInfo.] */
            /* SRS_X509_CERTIFICATES_21_002: [The constructor shall throw ArgumentException if the provided primary certificates is invalid.] */
            Primary = new X509CertificateWithInfo(primary);
            if (secondary != null)
            {
                Secondary = new X509CertificateWithInfo(secondary);
            }
            else
            {
                Secondary = null;
            }
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the <code>X509Certificates</code> using the provided set of certificate in Base64 string.
        /// </remarks>
        /// <param name="primary">the <code>string</code> with the provisioning certificate and info. It cannot be <code>null</code> or empty.</param>
        /// <param name="secondary">the <code>string</code> with the provisioning certificate and info. It can be <code>null</code> or empty.</param>
        /// <exception cref="ArgumentException">if the provided primary certificate is invalid.</exception>
        internal X509Certificates(string primary, string secondary = null)
        {
            /* SRS_X509_CERTIFICATES_21_001: [The constructor shall store the provided primary and secondary certificates as X509CertificateWithInfo.] */
            /* SRS_X509_CERTIFICATES_21_002: [The constructor shall throw ArgumentException if the provided primary certificates is invalid.] */
            Primary = new X509CertificateWithInfo(primary);
            if (secondary != null)
            {
                Secondary = new X509CertificateWithInfo(secondary);
            }
            else
            {
                Secondary = null;
            }
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the <code>X509Certificates</code> using the provided set of certificates in the JSON.
        /// </remarks>
        /// <param name="primary">the <see cref="X509CertificateWithInfo"/> with the provisioning certificate and info. It cannot be <code>null</code>.</param>
        /// <param name="secondary">the <see cref="X509CertificateWithInfo"/> with the provisioning certificate and info. It can be <code>null</code>.</param>
        /// <exception cref="ProvisioningServiceClientException">if the provided primary certificate is invalid.</exception>
        [JsonConstructor]
        private X509Certificates(X509CertificateWithInfo primary, X509CertificateWithInfo secondary = null)
        {
            /* SRS_X509_CERTIFICATES_21_001: [The constructor shall store the provided primary and secondary certificates as X509CertificateWithInfo.] */
            /* SRS_X509_CERTIFICATES_21_002: [The constructor shall throw ArgumentException if the provided primary certificates is invalid.] */
            Primary = primary ?? throw new ProvisioningServiceClientException("primary certificate cannot be null.");
            Secondary = secondary;
        }

        /// <summary>
        /// Primary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "primary")]
        public X509CertificateWithInfo Primary { get; private set; }

        /// <summary>
        /// Secondary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "secondary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CertificateWithInfo Secondary { get; private set; }
    }
}
