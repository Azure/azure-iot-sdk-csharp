// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Certificate with its info.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate that can contains the certificate,
    /// the info of the certificate or both.
    /// </remarks>
    public class X509CertificateWithInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <exception cref="ProvisioningServiceException"></exception>
        public X509CertificateWithInfo(X509Certificate2 certificate)
        {
            try
            {
                ValidateCertificate(certificate);

                Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
            }
            catch (CryptographicException ex)
            {
                throw new ProvisioningServiceException("The provided certificate is invalid.", HttpStatusCode.BadRequest, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <exception cref="ProvisioningServiceException"></exception>
        public X509CertificateWithInfo(string certificate)
        {
            try
            {
                ValidateCertificate(certificate);

                Certificate = certificate;
            }
            catch (CryptographicException ex)
            {
                throw new ProvisioningServiceException("The provided certificate is invalid.", HttpStatusCode.BadRequest, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="info"></param>
        [JsonConstructor]
        public X509CertificateWithInfo(string certificate, X509CertificateInfo info)
        {
            Certificate = certificate;
            Info = info;
        }

        /// <summary>
        /// Certificate
        /// </summary>
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        /// <summary>
        /// Certificate properties.
        /// </summary>
        [JsonPropertyName("info")]
        public X509CertificateInfo Info { get; set; }

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
            byte[] certBytes = Encoding.ASCII.GetBytes(certificate ?? throw new ArgumentException("Certificate cannot be null."));
            using var cert = new X509Certificate2(certBytes);
            ValidateCertificate(cert);
        }
    }
}
