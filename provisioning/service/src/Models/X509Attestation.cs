// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Attestation.
    /// </summary>
    /// <remarks>
    /// The provisioning service supports Device Identifier Composition Engine, or DICE, as the device attestation
    /// mechanism. To use X509, user must provide the certificate. This class provide the means to create a new
    /// attestation for a X509 certificate and return it as an abstract interface <see cref="Attestation"/>.
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
    /// For all operations over <see cref="IndividualEnrollment"/> with X509, user must provide a
    /// clientCertificates, and for operations over <see cref="EnrollmentGroup"/>, user must provide a
    /// rootCertificates or a X509CAReferences.
    ///
    /// For each of this types of certificates, user can provide 2 Certificates, a primary and a secondary. Only the
    /// primary is mandatory, the secondary is optional.
    ///
    /// The provisioning service will process the provided certificates, but will never return it back. Instead of
    /// it, <see cref="GetPrimaryX509CertificateInfo()"/> and <see cref="GetSecondaryX509CertificateInfo()"/>
    /// will return the certificate information for the certificates.
    /// </remarks>
    public class X509Attestation : Attestation
    {
        /// <summary>
        /// Client certificates.
        /// </summary>
        [JsonPropertyName("clientCertificates")]
        public X509Certificates ClientCertificates { get; set; }

        /// <summary>
        /// Signing certificates.
        /// </summary>
        [JsonPropertyName("signingCertificates")]
        public X509Certificates RootCertificates { get; set; }

        /// <summary>
        /// Certificates Authority references.
        /// </summary>
        [JsonPropertyName("caReferences")]
        public X509CaReferences CaReferences { get; set; }

        /// <summary>
        /// Getter for the primary X509 certificate info.
        /// </summary>
        /// <remarks>
        /// This method is a getter for the information returned from the provisioning service for the provided
        /// primary certificate.
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

            if (CaReferences != null)
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
    }
}
