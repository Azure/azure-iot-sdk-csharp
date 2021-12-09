// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Certificate with its info.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate that can contains the certificate,
    /// the info of the certificate or both.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <code>
    ///  {
    ///      "certificate": "-----BEGIN CERTIFICATE-----\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                     "-----END CERTIFICATE-----\n";
    ///  }
    /// </code>
    ///
    /// After send an X509 certificate to the provisioning service, it will return the <see cref="X509CertificateInfo"/>.
    /// User can get this info from this class,
    ///
    /// The following JSON is an example what info the provisioning service will return for X509.
    /// <code>
    ///  {
    ///      "info": {
    ///           "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///           "sha1Thumbprint": "0000000000000000000000000000000000",
    ///           "sha256Thumbprint": "validEnrollmentGroupId",
    ///           "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///           "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///           "notAfterUtc": "2017-11-20T12:34:18Z",
    ///           "serialNumber": "000000000000000000",
    ///           "version": 3
    ///      }
    ///  }
    /// </code>
    /// </example>
    public class X509CertificateWithInfo
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// This constructor will creates a new instance of the <see cref="X509CertificateWithInfo"/> using the 
        /// provided <see cref="X509Certificate2"/>.
        /// </remarks>
        /// <param name="certificate"> the <code>X509Certificate2"</code> with the provisioning certificate. It cannot be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if the provided certificate is <code>null</code>.</exception>
        internal X509CertificateWithInfo(X509Certificate2 certificate)
        {
            /* SRS_X509_CERTIFICATE_WITH_INFO_21_001: [The public constructor shall throws ArgumentException if the provided 
                                            certificate is null or CryptographicException if it is invalid.] */
            ValidateCertificate(certificate);

            /* SRS_X509_CERTIFICATE_WITH_INFO_21_002: [The public constructor shall store the provided certificate as Base64 string.] */
            Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

            /* SRS_X509_CERTIFICATE_WITH_INFO_21_003: [The public constructor shall set the Info to null.] */
            Info = null;
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// This constructor will creates a new instance of the <see cref="X509CertificateWithInfo"/> using the 
        /// provided Base64 string.
        /// </remarks>
        /// <param name="certificate">the <code>string</code> with the provisioning certificate. It cannot be <code>null</code> or invalid.</param>
        /// <exception cref="ArgumentException">if the provided certificate is <code>null</code>.</exception>
        internal X509CertificateWithInfo(string certificate)
        {
            /* SRS_X509_CERTIFICATE_WITH_INFO_21_001: [The public constructor shall throws ArgumentException if the provided 
                                            certificate is null or InvalidOperationException if it is invalid.] */
            ValidateCertificate(certificate);

            /* SRS_X509_CERTIFICATE_WITH_INFO_21_002: [The public constructor shall store the provided certificate as Base64 string.] */
            Certificate = certificate;

            /* SRS_X509_CERTIFICATE_WITH_INFO_21_003: [The public constructor shall set the Info to null.] */
            Info = null;
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the <see cref="X509CertificateWithInfo"/> using the provided set of info in the JSON.
        /// This method will validate each parameter and the object consistence.
        /// </remarks>
        /// 
        /// <param name="certificate">the <see cref="X509Certificate2"/> with the provisioning certificate. It can be <code>null</code>.</param>
        /// <param name="info">the X509 certificate properties returned form the provisioning service.</param>
        [JsonConstructor]
        private X509CertificateWithInfo(string certificate, X509CertificateInfo info)
        {
            /* SRS_X509_CERTIFICATE_WITH_INFO_21_004: [The constructor for JSON shall store the provided Info.] */
            Info = info;
            /* SRS_X509_CERTIFICATE_WITH_INFO_21_005: [The constructor for JSON shall store the provided Certificate.] */
            Certificate = certificate;
        }

        /// <summary>
        /// Certificate
        /// </summary>
        [JsonProperty(PropertyName = "certificate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Certificate { get; private set; }

        /// <summary>
        /// Certificate properties.
        /// </summary> 
        [JsonProperty(PropertyName = "info", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CertificateInfo Info { get; private set; }

        private static void ValidateCertificate(X509Certificate2 certificate)
        {
            if((certificate ?? throw new ArgumentException("Certificate cannot be null.")).HasPrivateKey)
            {
                throw new InvalidOperationException("Certificate should not contain a private key.");
            }
        }

        private static void ValidateCertificate(string certificate)
        {
            byte[] certBytes = System.Text.Encoding.ASCII.GetBytes(certificate ?? throw new ArgumentException("Certificate cannot be null."));
            var cert = new X509Certificate2(certBytes);
            ValidateCertificate(cert);
            cert.Dispose();
        }
    }
}
