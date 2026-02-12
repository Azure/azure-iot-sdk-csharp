// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Provides helper methods for working with certificates issued by DPS.
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
        /// <example>
        /// <code>
        /// DeviceRegistrationResult result = await provisioningClient.RegisterAsync(data);
        /// if (result.IssuedClientCertificate != null)
        /// {
        ///     string pemChain = CertificateHelper.ConvertToPem(result.IssuedClientCertificate);
        ///     File.WriteAllText("certificate_chain.pem", pemChain);
        /// }
        /// </code>
        /// </example>
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

#if NET6_0_OR_GREATER
        /// <summary>
        /// Creates an X509Certificate2 with private key from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain from DPS. The first element should be the leaf/device certificate.</param>
        /// <param name="privateKey">The RSA private key corresponding to the CSR.</param>
        /// <returns>X509Certificate2 with private key for IoT Hub authentication.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> or <paramref name="privateKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="certificateChain"/> is empty.</exception>
        /// <remarks>
        /// This method combines the issued leaf certificate with the private key used to generate the CSR.
        /// The resulting certificate can be used to authenticate with IoT Hub.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Generate key pair and CSR
        /// using var rsa = RSA.Create(2048);
        /// var request = new CertificateRequest($"CN={registrationId}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        /// string csrBase64 = Convert.ToBase64String(request.CreateSigningRequest());
        /// 
        /// // Register with DPS
        /// var data = new ProvisioningRegistrationAdditionalData { ClientCertificateSigningRequest = csrBase64 };
        /// DeviceRegistrationResult result = await provisioningClient.RegisterAsync(data);
        /// 
        /// // Create certificate with private key
        /// X509Certificate2 deviceCert = CertificateHelper.CreateCertificateWithPrivateKey(result.IssuedClientCertificate, rsa);
        /// </code>
        /// </example>
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

            // The first certificate in the chain is the leaf/device certificate
            byte[] leafCertBytes = Convert.FromBase64String(certificateChain[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);

            // Combine the certificate with the private key
            return leafCert.CopyWithPrivateKey(privateKey);
        }

        /// <summary>
        /// Creates an X509Certificate2 with private key from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain from DPS. The first element should be the leaf/device certificate.</param>
        /// <param name="privateKey">The ECDsa private key corresponding to the CSR.</param>
        /// <returns>X509Certificate2 with private key for IoT Hub authentication.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> or <paramref name="privateKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="certificateChain"/> is empty.</exception>
        /// <remarks>
        /// This method combines the issued leaf certificate with the private key used to generate the CSR.
        /// The resulting certificate can be used to authenticate with IoT Hub.
        /// ECC keys (e.g., P-256) are recommended for improved performance over RSA.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Generate ECC key pair and CSR
        /// using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        /// var request = new CertificateRequest($"CN={registrationId}", ecdsa, HashAlgorithmName.SHA256);
        /// string csrBase64 = Convert.ToBase64String(request.CreateSigningRequest());
        /// 
        /// // Register with DPS
        /// var data = new ProvisioningRegistrationAdditionalData { ClientCertificateSigningRequest = csrBase64 };
        /// DeviceRegistrationResult result = await provisioningClient.RegisterAsync(data);
        /// 
        /// // Create certificate with private key
        /// X509Certificate2 deviceCert = CertificateHelper.CreateCertificateWithPrivateKey(result.IssuedClientCertificate, ecdsa);
        /// </code>
        /// </example>
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

            // The first certificate in the chain is the leaf/device certificate
            byte[] leafCertBytes = Convert.FromBase64String(certificateChain[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);

            // Combine the certificate with the private key
            return leafCert.CopyWithPrivateKey(privateKey);
        }

        /// <summary>
        /// Creates an X509Certificate2Collection from the issued certificate chain.
        /// </summary>
        /// <param name="certificateChain">Issued certificate chain from DPS.</param>
        /// <returns>X509Certificate2Collection containing all certificates in the chain.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateChain"/> is null.</exception>
        /// <remarks>
        /// This method creates a collection of all certificates in the chain, which can be useful
        /// for scenarios where the entire chain needs to be presented or validated.
        /// </remarks>
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
#endif
    }
}
