﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

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
    /// <c>
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
    /// </c>
    ///
    /// After send an X509 certificate to the provisioning service, it will return the <see cref="X509CertificateInfo"/>.
    /// User can get this info from this class,
    ///
    /// The following JSON is an example what info the provisioning service will return for X509.
    /// <c>
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
    /// </c>
    /// </example>
    public class X509CertificateWithInfo
    {
        internal X509CertificateWithInfo(X509Certificate2 certificate)
        {
            ValidateCertificate(certificate);

            Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
            Info = null;
        }

        internal X509CertificateWithInfo(string certificate)
        {
            ValidateCertificate(certificate);

            Certificate = certificate;
            Info = null;
        }

        [JsonConstructor]
        private X509CertificateWithInfo(string certificate, X509CertificateInfo info)
        {
            Info = info;
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
            if (certificate == null)
            {
                throw new ArgumentException("Certificate cannot be null.");
            }

            if (certificate.HasPrivateKey)
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
