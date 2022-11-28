// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// For deserialization.
        /// </summary>
        internal X509CertificateWithInfo()
        { }

        /// <summary>
        /// Creates an instance of this object with th especified certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <exception cref="ArgumentException">When the certficate is null.</exception>
        /// <exception cref="ArgumentException">When the certicate is invalid; see inner exception.</exception>
        /// <exception cref="ArgumentException">When the certificate contains a private key.</exception>
        public X509CertificateWithInfo(X509Certificate2 certificate)
        {
            Argument.AssertNotNull(certificate, nameof(certificate));
            if (certificate.HasPrivateKey)
            {
                throw new ArgumentException("Certificate should not contain a private key.", nameof(certificate));
            }

            try
            {
                Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
            }
            catch (CryptographicException ex)
            {
                throw new ArgumentException("The provided certificate is invalid.", nameof(certificate), ex);
            }
        }

        /// <summary>
        /// Creates an instance of this class with the specified certficate body.
        /// </summary>
        /// <param name="certificate">The certficiate body.</param>
        /// <exception cref="ArgumentException">When the certicate is invalid; see inner exception.</exception>
        public X509CertificateWithInfo(string certificate)
        {
            Argument.AssertNotNull(certificate, nameof(certificate));

            try
            {
                // validate
                using var _ = new X509Certificate2(Encoding.ASCII.GetBytes(certificate));

                Certificate = certificate;
            }
            catch (CryptographicException ex)
            {
                throw new ArgumentException("The provided certificate is invalid.", nameof(certificate), ex);
            }
        }

        /// <summary>
        /// Certificate
        /// </summary>
        [JsonPropertyName("certificate")]
        public string Certificate { get; internal set; }

        /// <summary>
        /// Certificate properties.
        /// </summary>
        [JsonPropertyName("info")]
        public X509CertificateInfo Info { get; set; }
    }
}
