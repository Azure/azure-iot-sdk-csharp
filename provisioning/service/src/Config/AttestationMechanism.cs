// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service Attestation mechanism in the IndividualEnrollment and 
    ///     EnrollmentGroup.
    /// </summary>
    /// <remarks>
    /// It is an internal class that converts one of the attestations into JSON format. To configure
    ///     the attestation mechanism, see the external API <see cref="Attestation"/>.
    /// </remarks>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    public sealed class AttestationMechanism
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// It will create a new instance of the AttestationMechanism for the provided attestation type.
        /// </remarks>
        /// <param name="attestation">the <see cref="Attestation"/> with the TPM keys or X509 certificates. It cannot 
        ///     be <code>null</code>.</param>
        /// <exception cref="ArgumentNullException">If the provided attestation is <code>null</code>.</exception> 
        internal AttestationMechanism(Attestation attestation)
        {
            /* SRS_ATTESTATION_MECHANISM_21_001: [The constructor shall throw ArgumentNullException if the provided attestation is null.] */
            if (attestation == null)
            {
                throw new ArgumentNullException("Attestation cannot be null");
            }

            if (attestation is TpmAttestation)
            {
                /* SRS_ATTESTATION_MECHANISM_21_002: [If the provided attestation is instance of TpmAttestation, the constructor 
                                                        shall store the provided TPM keys.] */
                Tpm = (TpmAttestation)attestation;
            }
            else if (attestation is X509Attestation)
            {
                /* SRS_ATTESTATION_MECHANISM_21_006: [If the provided attestation is instance of X509Attestation, the constructor 
                                                        shall store the provided X509 certificates.] */
                X509 = (X509Attestation)attestation;
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
        /// <exception cref="ProvisioningServiceClientException">if the received JSON is invalid.</exception>
        [JsonConstructor]
        private AttestationMechanism(AttestationMechanismType type, TpmAttestation tpm, X509Attestation x509)
        {
            switch(type)
            {
                case AttestationMechanismType.Tpm:
                    if(tpm == null)
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
                    if(x509 == null)
                    {
                        /* SRS_ATTESTATION_MECHANISM_21_015: [The constructor shall throw ProvisioningServiceClientException if the  
                                                    provided AttestationMechanismType is `X509` but the X509 attestation is null.] */
                        throw new ProvisioningServiceClientException("Invalid X509 attestation mechanism.");
                    }

                    /* SRS_ATTESTATION_MECHANISM_21_016: [If the provided AttestationMechanismType is `X509`, the constructor 
                                                    shall store the provided X509 attestation.] */
                    X509 = x509;
                    break;
                default:
                    /* SRS_ATTESTATION_MECHANISM_21_017: [The constructor shall throw ProvisioningServiceClientException if the 
                                                    provided ProvisioningServiceClientException is not `TPM` or `X509`.] */
                    throw new ProvisioningServiceClientException("Unknown attestation mechanism.");
            }
        }

        internal Attestation GetAttestation()
        {
            switch(Type)
            {
                case AttestationMechanismType.Tpm:
                    /* SRS_ATTESTATION_MECHANISM_21_010: [If the type is `TPM`, the getAttestation shall return the 
                                                    stored TpmAttestation.] */
                    return _tpm;
                case AttestationMechanismType.X509:
                    /* SRS_ATTESTATION_MECHANISM_21_011: [If the type is `X509`, the getAttestation shall return the 
                                                    stored X509Attestation.] */
                    return _x509;
                default:
                    /* SRS_ATTESTATION_MECHANISM_21_012: [If the type is not `X509` or `TPM`, the getAttestation shall 
                                                    throw ProvisioningServiceClientException.] */
                    throw new ProvisioningServiceClientException("Unknown Attestation type"); 
            }
        }

        /// <summary>
        ///  Attestation Type.
        /// </summary>
        [DefaultValue(AttestationMechanismType.None)]
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
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
                /* SRS_ATTESTATION_MECHANISM_21_003: [If the provided attestation is instance of TpmAttestation, the 
                                                        constructor shall set the attestation type as TPM.] */
                this.Type = AttestationMechanismType.Tpm;
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
                /* SRS_ATTESTATION_MECHANISM_21_007: [If the provided attestation is instance of X509Attestation, the 
                                                        constructor shall set the attestation type as X509.] */
                this.Type = AttestationMechanismType.X509;
            }
        }
        private X509Attestation _x509;
    }
}
