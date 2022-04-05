// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service Attestation mechanism of an IndividualEnrollment or an EnrollmentGroup.
    /// </summary>
    public sealed class AttestationMechanism
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// It will create a new instance of the AttestationMechanism for the provided attestation type.
        /// </remarks>
        /// <param name="attestation">the <see cref="Attestation"/> with the TPM keys, X509 certificates, or Symmetric keys. It cannot
        ///     be <code>null</code>.</param>
        /// <exception cref="ArgumentNullException">If the provided attestation is <code>null</code>.</exception>
        internal AttestationMechanism(Attestation attestation)
        {
            /* SRS_ATTESTATION_MECHANISM_21_001: [The constructor shall throw ArgumentNullException if the provided attestation is null.] */
            if (attestation == null)
            {
                throw new ArgumentNullException(nameof(attestation));
            }

            if (attestation is TpmAttestation)
            {
                Tpm = (TpmAttestation)attestation;
            }
            else if (attestation is X509Attestation)
            {
                X509 = (X509Attestation)attestation;
            }
            else if (attestation is NoneAttestation)
            {
                // No-op.
            }
            else if (attestation is SymmetricKeyAttestation)
            {
                SymmetricKey = (SymmetricKeyAttestation)attestation;
            }
            else
            {
                /* SRS_ATTESTATION_MECHANISM_21_005: [The constructor shall throw ArgumentException if the provided attestation is
                                                        unknown.] */
                throw new ArgumentException("Unknown attestation mechanism");
            }
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Constructor for JSON. It will receive the attestation and the attestation type, check if it is correct
        /// and store the information.
        /// </remarks>
        /// <param name="type">the <see cref="AttestationMechanismType"/> identifying each attestation the provisioning service
        ///     is using.</param>
        /// <param name="tpm">the <see cref="TpmAttestation"/> with the TPM keys.</param>
        /// <param name="x509">the <see cref="X509Attestation"/> with the certificate information.</param>
        /// <param name="symmetricKey">the <see cref="SymmetricKeyAttestation"/> with the primary and secondary key.</param>
        /// <exception cref="ProvisioningServiceClientException">if the received JSON is invalid.</exception>
        [JsonConstructor]
        private AttestationMechanism(AttestationMechanismType type, TpmAttestation tpm, X509Attestation x509, SymmetricKeyAttestation symmetricKey)
        {
            switch (type)
            {
                case AttestationMechanismType.Tpm:
                    if (tpm == null)
                    {
                        /* SRS_ATTESTATION_MECHANISM_21_013: [The constructor shall throw ProvisioningServiceClientException if the
                                                        provided AttestationMechanismType is `TPM` but the TPM attestation is null.] */
                        throw new ProvisioningServiceClientException("Invalid TPM attestation mechanism.");
                    }

                    /* SRS_ATTESTATION_MECHANISM_21_014: [If the provided AttestationMechanismType is `TPM`, the constructor
                                                        shall store the provided TPM attestation.] */
                    Tpm = tpm;
                    break;

                case AttestationMechanismType.X509:
                    if (x509 == null)
                    {
                        /* SRS_ATTESTATION_MECHANISM_21_015: [The constructor shall throw ProvisioningServiceClientException if the
                                                    provided AttestationMechanismType is `X509` but the X509 attestation is null.] */
                        throw new ProvisioningServiceClientException("Invalid X509 attestation mechanism.");
                    }

                    /* SRS_ATTESTATION_MECHANISM_21_016: [If the provided AttestationMechanismType is `X509`, the constructor
                                                    shall store the provided X509 attestation.] */
                    X509 = x509;
                    break;

                case AttestationMechanismType.None:
                    break;

                case AttestationMechanismType.SymmetricKey:
                    // In some cases symmetric keys are nulled out by the service
                    if (symmetricKey == null)
                    {
                        SymmetricKey = new SymmetricKeyAttestation(string.Empty, string.Empty);
                    }
                    else
                    {
                        SymmetricKey = symmetricKey;
                    }
                    break;

                default:
                    /* SRS_ATTESTATION_MECHANISM_21_017: [The constructor shall throw ProvisioningServiceClientException if the
                                                    provided ProvisioningServiceClientException is not `TPM`, `X509` or `SymmetricKey`.] */
                    throw new ProvisioningServiceClientException("Unknown attestation mechanism.");
            }
        }

        /// <summary>
        /// Get the attestation of this object. The returned attestation may be of type
        /// <see cref="SymmetricKeyAttestation"/>, <see cref="X509Attestation"/>, or <see cref="TpmAttestation"/>.
        /// By casting the returned Attestation to the appropriate attestation type, you can access the x509/symmetric key/tpm
        /// specific attestation fields. Use <see cref="Type"/> to cast this field to the appropriate attestation type.
        /// </summary>
        /// <returns>The attestation of this object.</returns>
        public Attestation GetAttestation()
        {
            switch (Type)
            {
                case AttestationMechanismType.Tpm:
                    return _tpm;

                case AttestationMechanismType.X509:
                    return _x509;

                case AttestationMechanismType.None:
                    return s_none;

                case AttestationMechanismType.SymmetricKey:
                    return _symmetricKey;

                default:
                    /* SRS_ATTESTATION_MECHANISM_21_012: [If the type is not `X509`, `TPM` or `SymmetricKey`, the getAttestation shall
                                                    throw ProvisioningServiceClientException.] */
                    throw new ProvisioningServiceClientException("Unknown Attestation type");
            }
        }

        /// <summary>
        ///  Attestation Type.
        /// </summary>
        [DefaultValue(AttestationMechanismType.None)]
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public AttestationMechanismType Type { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TpmAttestation"/> used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "tpm", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private TpmAttestation Tpm
        {
            get
            {
                return _tpm;
            }

            set
            {
                _tpm = value;
                /* SRS_ATTESTATION_MECHANISM_21_004: [If the provided attestation is instance of TpmAttestation, the
                                                        constructor shall set the x508 as null.] */
                _x509 = null;
                _symmetricKey = null;

                /* SRS_ATTESTATION_MECHANISM_21_003: [If the provided attestation is instance of TpmAttestation, the
                                                        constructor shall set the attestation type as TPM.] */
                Type = AttestationMechanismType.Tpm;
            }
        }

        private TpmAttestation _tpm;

        /// <summary>
        /// Gets or sets the <see cref="X509Attestation"/> used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "x509", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private X509Attestation X509
        {
            get
            {
                return _x509;
            }

            set
            {
                _x509 = value;
                /* SRS_ATTESTATION_MECHANISM_21_008: [If the provided attestation is instance of X509Attestation, the
                                                        constructor shall set the TPM as null.] */
                _tpm = null;
                _symmetricKey = null;

                /* SRS_ATTESTATION_MECHANISM_21_007: [If the provided attestation is instance of X509Attestation, the
                                                        constructor shall set the attestation type as X509.] */
                Type = AttestationMechanismType.X509;
            }
        }

        private X509Attestation _x509;

        [JsonProperty(PropertyName = "symmetricKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private SymmetricKeyAttestation SymmetricKey
        {
            get
            {
                return _symmetricKey;
            }

            set
            {
                _symmetricKey = value;
                _x509 = null;
                _tpm = null;

                Type = AttestationMechanismType.SymmetricKey;
            }
        }

        private SymmetricKeyAttestation _symmetricKey;

        private class NoneAttestation : Attestation { }

        private static NoneAttestation s_none = new NoneAttestation();
    }
}
