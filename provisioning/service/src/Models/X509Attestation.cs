// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Attestation.
    /// </summary>
    /// <remarks>
    /// The provisioning service supports Device Identifier Composition Engine, or DICE, as the device attestation
    ///     mechanism. To use X509, user must provide the certificate. This class provide the means to create a new
    ///     attestation for a X509 certificate and return it as an abstract interface <see cref="Attestation"/>.
    ///
    /// An X509 attestation can contains one of the 3 types of certificate:
    ///
    /// <list type="bullet">
    ///     <item>
    ///     <description>Client or Alias certificate:
    ///         Called on this class as clientCertificates, this certificate can authenticate a single device.</description>
    ///     </item>
    ///     <item>
    ///     <description>Signing or Root certificate:
    ///           Called on this class as rootCertificates, this certificate can create multiple Client certificates
    ///           to authenticate multiple devices.</description>
    ///     </item>
    ///     <item>
    ///     <description>CA Reference:
    ///           Called on this class as X509CAReferences, this is a CA reference for a rootCertificate that can
    ///           creates multiple Client certificates to authenticate multiple devices.</description>
    ///     </item>
    /// </list>
    ///
    /// The provisioning service allows user to create <see cref="IndividualEnrollment"/> and <see cref="EnrollmentGroup"/>.
    ///     For all operations over <see cref="IndividualEnrollment"/> with X509, user must provide a
    ///     clientCertificates, and for operations over <see cref="EnrollmentGroup"/>, user must provide a
    ///     rootCertificates or a X509CAReferences.
    ///
    /// For each of this types of certificates, user can provide 2 Certificates, a primary and a secondary. Only the
    ///     primary is mandatory, the secondary is optional.
    ///
    /// The provisioning service will process the provided certificates, but will never return it back. Instead of
    ///     it, <see cref="GetPrimaryX509CertificateInfo()"/> and <see cref="GetSecondaryX509CertificateInfo()"/>
    ///     will return the certificate information for the certificates.
    /// </remarks>
    public sealed class X509Attestation : Attestation
    {
        /// <summary>
        /// Client certificates.
        /// </summary>
        [JsonProperty(PropertyName = "clientCertificates", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal X509Certificates ClientCertificates { get; private set; }

        /// <summary>
        /// Signing certificates.
        /// </summary>
        [JsonProperty(PropertyName = "signingCertificates", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal X509Certificates RootCertificates { get; private set; }

        /// <summary>
        /// Certificates Authority references.
        /// </summary>
        [JsonProperty(PropertyName = "caReferences", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CAReferences CAReferences { get; private set; }

        /// <summary>
        /// Factory from ClientCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in X509Certificate2 object.
        /// </remarks>
        /// <param name="primary">The X509Certificate2 with the primary certificate. It cannot be null.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="primary"/> is null.</exception>
        /// <exception cref="CryptographicException">If the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(X509Certificate2 primary)
        {
            Argument.AssertNotNull(primary, nameof(primary));
            return new X509Attestation(
                new X509Certificates(primary, null),
                null, null);
        }

        /// <summary>
        /// Factory from ClientCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in X509Certificate2 objects.
        /// </remarks>
        /// <param name="primary">The X509Certificate2 with the primary certificate. It cannot be null.</param>
        /// <param name="secondary">The X509Certificate2 with the secondary certificate. It can be null (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="primary"/> is null.</exception>
        /// <exception cref="CryptographicException">If the one of the provided <paramref name="primary"/> or <paramref name="secondary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(X509Certificate2 primary, X509Certificate2 secondary)
        {
            Argument.AssertNotNull(primary, nameof(primary));
            return new X509Attestation(
                new X509Certificates(primary, secondary),
                null, null);
        }

        /// <summary>
        /// Factory from ClientCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in a Base64 string.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(string primary)
        {
            Argument.AssertNotNullOrEmpty(primary, nameof(primary));

            return new X509Attestation(
                new X509Certificates(primary, null),
                null, null);
        }

        /// <summary>
        /// Factory from ClientCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in a Base64 string.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <param name="secondary">The string with the secondary certificate. It can be null or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the one of the provided <paramref name="primary"/> or <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(string primary, string secondary)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));

            return new X509Attestation(
                new X509Certificates(primary, secondary),
                null, null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in X509Certificate2 objects.
        /// </remarks>
        /// <param name="primary">The X509Certificate2 with the primary certificate. It cannot be null.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="primary"/> is null.</exception>
        /// <exception cref="CryptographicException">If the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(X509Certificate2 primary)
        {
            Argument.AssertNotNull(primary, nameof(primary));
            return new X509Attestation(
                null,
                new X509Certificates(primary, null),
                null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in X509Certificate2 objects.
        /// </remarks>
        /// <param name="primary">The X509Certificate2 with the primary certificate. It cannot be null.</param>
        /// <param name="secondary">The X509Certificate2 with the secondary certificate. It can be null (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="primary"/> is null.</exception>
        /// <exception cref="CryptographicException">If the one of the provided <paramref name="primary"/> or <paramref name="secondary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(X509Certificate2 primary, X509Certificate2 secondary)
        {
            Argument.AssertNotNull(primary, nameof(primary));
            return new X509Attestation(
                null,
                new X509Certificates(primary, secondary),
                null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in Base64 string.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(string primary)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));

            return new X509Attestation(
                null,
                new X509Certificates(primary, null),
                null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in Base64 string.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <param name="secondary">The string with the secondary certificate. It can be null or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the one of the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(string primary, string secondary)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));

            return new X509Attestation(
                null,
                new X509Certificates(primary, secondary),
                null);
        }

        /// <summary>
        /// Factory with CAReferences with primary CA references.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary CA reference.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the provided <paramref name="primary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromCAReferences(string primary)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));

            return new X509Attestation(
                null, null,
                new X509CAReferences(primary, null));
        }

        /// <summary>
        /// Factory with CAReferences with primary and secondary CA references.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary CA reference.
        /// </remarks>
        /// <param name="primary">The string with the primary certificate. It cannot be null or empty.</param>
        /// <param name="secondary">The string with the secondary certificate. It can be null or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="primary"/> string is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="primary"/> string is empty or white space.</exception>
        /// <exception cref="CryptographicException">If the one of the provided <paramref name="primary"/> or <paramref name="secondary"/> certificate is invalid.</exception>
        public static X509Attestation CreateFromCAReferences(string primary, string secondary)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));

            return new X509Attestation(
                null, null,
                new X509CAReferences(primary, secondary));
        }

        /// <summary>
        /// Getter for the primary X509 certificate info.
        /// </summary>
        /// <remarks>
        /// This method is a getter for the information returned from the provisioning service for the provided
        ///     primary certificate.
        /// </remarks>
        /// <returns>The <see cref="X509CertificateInfo"/> with the returned certificate information. it can be null.</returns>
        /// <exception cref="InvalidOperationException">If no valid certificate information was provided on initialization.</exception>
        public X509CertificateInfo GetPrimaryX509CertificateInfo()
        {
            if (ClientCertificates != null)
            {
                return ClientCertificates.Primary.Info;
            }

            if (RootCertificates != null)
            {
                return RootCertificates.Primary.Info;
            }

            if (CAReferences != null)
            {
                return null;
            }

            throw new InvalidOperationException("No valid certificate information was provided on initialization.");
        }

        /// <summary>
        /// Getter for the secondary X509 certificate info.
        /// </summary>
        /// <remarks>
        /// This method is a getter for the information returned from the provisioning service for the provided
        /// secondary certificate.
        /// </remarks>
        /// <returns>The <see cref="X509CertificateInfo"/> with the returned certificate information. it can be null.</returns>
        public X509CertificateInfo GetSecondaryX509CertificateInfo()
        {
            X509CertificateWithInfo secondaryCertificate = null;

            if (ClientCertificates != null)
            {
                secondaryCertificate = ClientCertificates.Secondary;
            }
            else if (RootCertificates != null)
            {
                secondaryCertificate = RootCertificates.Secondary;
            }

            return secondaryCertificate?.Info;
        }

        [JsonConstructor]
        private X509Attestation(
            X509Certificates clientCertificates, X509Certificates rootCertificates, X509CAReferences caReferences)
        {
            if (clientCertificates == null
                && rootCertificates == null
                && caReferences == null)
            {
                throw new DeviceProvisioningServiceException("Attestation shall receive one non-null certificate.", HttpStatusCode.BadRequest);
            }

            if (clientCertificates != null
                && (rootCertificates != null
                    || caReferences != null)
                || rootCertificates != null
                && caReferences != null)
            {
                throw new DeviceProvisioningServiceException("Attestation cannot receive more than one certificate.", HttpStatusCode.BadRequest);
            }

            try
            {
                ClientCertificates = clientCertificates;
                RootCertificates = rootCertificates;
                CAReferences = caReferences;
            }
            catch (ArgumentException e)
            {
                throw new DeviceProvisioningServiceException(e);
            }
        }
    }
}
