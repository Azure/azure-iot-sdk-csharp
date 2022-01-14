// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
    ///     <description><b>Client or Alias certificate:</b>
    ///         Called on this class as clientCertificates, this certificate can authenticate a single device.</description>
    ///     </item>
    ///     <item>
    ///     <description><b>Signing or Root certificate:</b>
    ///           Called on this class as rootCertificates, this certificate can create multiple Client certificates
    ///           to authenticate multiple devices.</description>
    ///     </item>
    ///     <item>
    ///     <description><b>CA Reference:</b>
    ///           Called on this class as X509CAReferences, this is a CA reference for a rootCertificate that can
    ///           creates multiple Client certificates to authenticate multiple devices.</description>
    ///     </item>
    /// </list>
    ///
    /// The provisioning service allows user to create <see cref="IndividualEnrollment"/> and <see cref="EnrollmentGroup"/>. 
    ///     For all operations over <see cref="IndividualEnrollment"/> with <b>X509</b>, user must provide a 
    ///     <b>clientCertificates</b>, and for operations over <see cref="EnrollmentGroup"/>, user must provide a 
    ///     <b>rootCertificates</b> or a <b>X509CAReferences</b>.
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
        /// <param name="primary">the <code>X509Certificate2</code> with the primary certificate. It cannot be <code>null</code>.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code>.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(X509Certificate2 primary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_003: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the ClientCertificates.] */
            return new X509Attestation(
                new X509Certificates(
                    primary ?? throw new ArgumentException("primary certificate cannot be null."),
                    null), 
                null, null);
        }

        /// <summary>
        /// Factory from ClientCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in X509Certificate2 objects.
        /// </remarks>
        /// <param name="primary">the <code>X509Certificate2</code> with the primary certificate. It cannot be <code>null</code>.</param>
        /// <param name="secondary">the <code>X509Certificate2</code> with the secondary certificate. It can be <code>null</code> (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code>.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(X509Certificate2 primary, X509Certificate2 secondary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_003: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the ClientCertificates.] */
            return new X509Attestation(
                new X509Certificates(
                    primary ?? throw new ArgumentException("primary certificate cannot be null."),
                    secondary),
                null, null);
        }

        /// <summary>
        /// Factory from ClientCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in a Base64 string.
        /// </remarks>
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code> or empty.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(string primary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            if(string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_003: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the ClientCertificates.] */
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
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <param name="secondary">the <code>string</code> with the secondary certificate. It can be <code>null</code> or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code> or empty.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromClientCertificates(string primary, string secondary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            if (string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the 
                                            provided primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_003: [The factory shall create a new instance of the X509Attestation with the 
                                            created X509Certificates as the ClientCertificates.] */
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
        /// <param name="primary">the <code>X509Certificate2</code> with the primary certificate. It cannot be <code>null</code>.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code>.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(X509Certificate2 primary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_004: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the RootCertificates.] */
            return new X509Attestation(
                null,
                new X509Certificates(
                    primary ?? throw new ArgumentException("primary certificate cannot be null."),
                    null),
                null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary and secondary certificates.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary and secondary certificates in X509Certificate2 objects.
        /// </remarks>
        /// <param name="primary">the <code>X509Certificate2</code> with the primary certificate. It cannot be <code>null</code>.</param>
        /// <param name="secondary">the <code>X509Certificate2</code> with the secondary certificate. It can be <code>null</code> (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code>.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(X509Certificate2 primary, X509Certificate2 secondary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_004: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the RootCertificates.] */
            return new X509Attestation(
                null,
                new X509Certificates(
                    primary ?? throw new ArgumentException("primary certificate cannot be null."),
                    secondary),
                null);
        }

        /// <summary>
        /// Factory from RootCertificates with primary certificate.
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation with the primary certificate in Base64 string.
        /// </remarks>
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code> or empty.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(string primary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            if (string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_004: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the RootCertificates.] */
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
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <param name="secondary">the <code>string</code> with the secondary certificate. It can be <code>null</code> or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the primary certificate is <code>null</code> or empty.</exception>
        /// <exception cref="CryptographicException">if the one of the provided certificate is invalid.</exception>
        public static X509Attestation CreateFromRootCertificates(string primary, string secondary)
        {
            /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
            if (string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary certificates.] */
            /* SRS_X509_ATTESTATION_21_004: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the RootCertificates.] */
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
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the provide primary certificate is invalid.</exception>
        public static X509Attestation CreateFromCAReferences(string primary)
        {
            /* SRS_X509_ATTESTATION_21_005: [The factory shall throws ArgumentException if the primary CA reference is null or empty.] */
            if (string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_006: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary CA reference.] */
            /* SRS_X509_ATTESTATION_21_007: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the caReference.] */
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
        /// <param name="primary">the <code>string</code> with the primary certificate. It cannot be <code>null</code> or empty.</param>
        /// <param name="secondary">the <code>string</code> with the secondary certificate. It can be <code>null</code> or empty (ignored).</param>
        /// <returns>The new instance of the X509Attestation.</returns>
        /// <exception cref="ArgumentException">if the provide primary certificate is invalid.</exception>
        public static X509Attestation CreateFromCAReferences(string primary, string secondary)
        {
            /* SRS_X509_ATTESTATION_21_005: [The factory shall throws ArgumentException if the primary CA reference is null or empty.] */
            if (string.IsNullOrWhiteSpace(primary))
            {
                throw new ArgumentException("primary certificate cannot be null or empty.");
            }
            /* SRS_X509_ATTESTATION_21_006: [The factory shall create a new instance of the X509Certificates with the provided 
                                            primary and secondary CA reference.] */
            /* SRS_X509_ATTESTATION_21_007: [The factory shall create a new instance of the X509Attestation with the created 
                                            X509Certificates as the caReference.] */
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
        /// <returns>The <see cref="X509CertificateInfo"/> with the returned certificate information. it can be <code>null</code>.</returns>
        public X509CertificateInfo GetPrimaryX509CertificateInfo()
        {
            /* SRS_X509_ATTESTATION_21_008: [If the ClientCertificates is not null, the GetPrimaryX509CertificateInfo shall 
                                            return the info in the Primary key of the ClientCertificates.] */
            if (ClientCertificates != null)
            {
                return ClientCertificates.Primary.Info;
            }
            /* SRS_X509_ATTESTATION_21_009: [If the RootCertificates is not null, the GetPrimaryX509CertificateInfo shall 
                                            return the info in the Primary key of the RootCertificates.] */
            if (RootCertificates != null)
            {
                return RootCertificates.Primary.Info;
            }
            /* SRS_X509_ATTESTATION_21_010: [If the CAReferences is not null, the GetPrimaryX509CertificateInfo shall return null.] */
            if (CAReferences != null)
            {
                return null;
            }
            /* SRS_X509_ATTESTATION_21_011: [If ClientCertificates, RootCertificates, and CAReferences are null, the 
                                            GetPrimaryX509CertificateInfo shall throw ArgumentException.] */
            throw new ArgumentException("There is no valid certificate information.");
        }

        /// <summary>
        /// Getter for the secondary X509 certificate info.
        /// </summary>
        /// <remarks>
        /// This method is a getter for the information returned from the provisioning service for the provided
        ///     secondary certificate.
        /// </remarks>
        /// <returns>The <see cref="X509CertificateInfo"/> with the returned certificate information. it can be <code>null</code>.</returns>
        public X509CertificateInfo GetSecondaryX509CertificateInfo()
        {
            X509CertificateWithInfo secondaryCertificate = null;
            /* SRS_X509_ATTESTATION_21_012: [If the ClientCertificates is not null, and it contains Secondary key, the 
                                            GetSecondaryX509CertificateInfo shall return the info in the Secondary key 
                                            of the ClientCertificates.] */
            if (ClientCertificates != null)
            {
                secondaryCertificate = ClientCertificates.Secondary;
            }
            /* SRS_X509_ATTESTATION_21_013: [If the RootCertificates is not null, and it contains Secondary key, the 
                                            GetSecondaryX509CertificateInfo shall return the info in the Secondary key 
                                            of the RootCertificates.] */
            else if (RootCertificates != null)
            {
                secondaryCertificate = RootCertificates.Secondary;
            }

            if (secondaryCertificate != null)
            {
                return secondaryCertificate.Info;
            }
            return null;
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509Attestation using one of the 3 certificates types. 
        /// <b>Note:</b> This constructor requires one, and only one certificate type.
        /// </remarks>
        /// <param name="clientCertificates">the <see cref="X509Certificates"/> with the primary and secondary certificates for 
        ///     Individual IndividualEnrollment.</param>
        /// <param name="rootCertificates">the <see cref="X509Certificates"/> with the primary and secondary certificates for 
        ///     Enrollment Group.</param>
        /// <param name="caReferences">the <see cref="X509CAReferences"/> with the primary and secondary CA references for 
        ///     Enrollment Group.</param>
        /// <exception cref="ProvisioningServiceClientException">if non certificate is provided or more than one certificates are provided.</exception>
        [JsonConstructor]
        private X509Attestation(
            X509Certificates clientCertificates, X509Certificates rootCertificates, X509CAReferences caReferences)
        {
            /* SRS_X509_ATTESTATION_21_014: [The constructor shall throws ArgumentException if `clientCertificates`, 
                                            `rootCertificates`, and `caReferences` are null.] */
            if ((clientCertificates == null) && (rootCertificates == null) && (caReferences == null))
            {
                throw new ProvisioningServiceClientException("Attestation shall receive one no null Certificate");
            }
            /* SRS_X509_ATTESTATION_21_015: [The constructor shall throws ArgumentException if more than one 
                                            certificate type are not null.] */
            if (((clientCertificates != null) && ((rootCertificates != null) || (caReferences != null))) || 
                ((rootCertificates != null) && (caReferences != null)))
            {
                throw new ProvisioningServiceClientException("Attestation cannot receive more than one certificate together");
            }

            /* SRS_X509_ATTESTATION_21_016: [The constructor shall store the provided `clientCertificates`, 
                                            `rootCertificates`, and `caReferences`.] */

            try
            {
                ClientCertificates = clientCertificates;
                RootCertificates = rootCertificates;
                CAReferences = caReferences;
            }
            catch (ArgumentException e)
            {
                throw new ProvisioningServiceClientException(e);
            }
        }

    }
}
