// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Provides helper methods for working with certificates issued by IoT Hub or DPS.
    /// </summary>
    public static class CertificateHelper
    {
        private const string BeginCertificate = "-----BEGIN CERTIFICATE-----";
        private const string EndCertificate = "-----END CERTIFICATE-----";

        /// <summary>
        /// Converts a list of base64-encoded certificates to PEM format.
        /// </summary>
        /// <param name="certificateChain">Array of base64-encoded certificates.</param>
        /// <returns>PEM-formatted certificate chain string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> is null.</exception>
        public static string ConvertToPem(IReadOnlyList<string> certificateChain)
        {
            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }

            var sb = new StringBuilder();
            foreach (string cert in certificateChain)
            {
                sb.AppendLine(BeginCertificate);
                sb.AppendLine(cert);
                sb.AppendLine(EndCertificate);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates an X509Certificate2 with private key from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain. The first element should be the leaf/device certificate.</param>
        /// <param name="privateKey">The RSA private key corresponding to the CSR.</param>
        /// <returns>X509Certificate2 with private key for IoT Hub authentication.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> or <paramref name="privateKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="certificateChain"/> is empty.</exception>
        public static X509Certificate2 CreateCertificateWithPrivateKey(
            IReadOnlyList<string> certificateChain,
            RSA privateKey)
        {
            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }

            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (certificateChain.Count == 0)
            {
                throw new ArgumentException("Certificate chain cannot be empty.", nameof(certificateChain));
            }

            byte[] leafCertBytes = Convert.FromBase64String(certificateChain[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);
            return leafCert.CopyWithPrivateKey(privateKey);
        }

        /// <summary>
        /// Creates an X509Certificate2 with private key from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain. The first element should be the leaf/device certificate.</param>
        /// <param name="privateKey">The ECDsa private key corresponding to the CSR.</param>
        /// <returns>X509Certificate2 with private key for IoT Hub authentication.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> or <paramref name="privateKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="certificateChain"/> is empty.</exception>
        public static X509Certificate2 CreateCertificateWithPrivateKey(
            IReadOnlyList<string> certificateChain,
            ECDsa privateKey)
        {
            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }

            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (certificateChain.Count == 0)
            {
                throw new ArgumentException("Certificate chain cannot be empty.", nameof(certificateChain));
            }

            byte[] leafCertBytes = Convert.FromBase64String(certificateChain[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);
            return leafCert.CopyWithPrivateKey(privateKey);
        }

        /// <summary>
        /// Creates an X509Certificate2Collection from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain.</param>
        /// <returns>X509Certificate2Collection containing all certificates in the chain.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> is null.</exception>
        public static X509Certificate2Collection CreateCertificateCollection(IReadOnlyList<string> certificateChain)
        {
            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }

            var collection = new X509Certificate2Collection();
            foreach (string certBase64 in certificateChain)
            {
                byte[] certBytes = Convert.FromBase64String(certBase64);
                collection.Add(new X509Certificate2(certBytes));
            }

            return collection;
        }
    }
}

