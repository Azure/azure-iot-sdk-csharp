﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service Attestation mechanism of an IndividualEnrollment or an EnrollmentGroup.
    /// </summary>
    public sealed class AttestationMechanism
    {
        private X509Attestation _x509;
        private SymmetricKeyAttestation _symmetricKey;
        private TpmAttestation _tpm;

        private static readonly NoneAttestation s_none = new();

        internal AttestationMechanism(Attestation attestation)
        {
            if (attestation == null)
            {
                throw new ArgumentNullException(nameof(attestation));
            }

            if (attestation is X509Attestation x509Attestation)
            {
                X509 = x509Attestation;
            }
            else if (attestation is NoneAttestation)
            {
                // No-op.
            }
            else if (attestation is SymmetricKeyAttestation symmetricKeyAttestation)
            {
                SymmetricKey = symmetricKeyAttestation;
            }
            else if (attestation is TpmAttestation tpmAttestion)
            {
                Tpm = tpmAttestion;
            }
            else
            {
                throw new ArgumentException("Unknown attestation mechanism");
            }
        }

        [JsonConstructor]
        private AttestationMechanism(AttestationMechanismType type, X509Attestation x509, TpmAttestation tpm, SymmetricKeyAttestation symmetricKey)
        {
            switch (type)
            {
                case AttestationMechanismType.X509:
                    if (x509 == null)
                    {
                        throw new ProvisioningServiceException("Invalid X509 attestation mechanism.", HttpStatusCode.BadRequest);
                    }

                    X509 = x509;
                    break;

                case AttestationMechanismType.None:
                    break;

                case AttestationMechanismType.SymmetricKey:
                    // In some cases symmetric keys are nulled out by the service
                    SymmetricKey = symmetricKey ?? new SymmetricKeyAttestation(string.Empty, string.Empty);
                    break;

                case AttestationMechanismType.Tpm:
                    if (tpm == null)
                    {
                        throw new ProvisioningServiceException("Invalid TPM attestation mechanism.", HttpStatusCode.BadRequest);
                    }

                    Tpm = tpm;
                    break;

                default:
                    throw new ProvisioningServiceException("Unknown attestation mechanism.", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        ///  Attestation Type.
        /// </summary>
        [DefaultValue(AttestationMechanismType.None)]
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AttestationMechanismType Type { get; set; }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonPropertyName("x509")]
        private X509Attestation X509
        {
            get => _x509;

            set
            {
                _x509 = value;
                _symmetricKey = null;
                _tpm = null;
                Type = AttestationMechanismType.X509;
            }
        }

        [JsonPropertyName("symmetricKey")]
        private SymmetricKeyAttestation SymmetricKey
        {
            get => _symmetricKey;

            set
            {
                _symmetricKey = value;
                _x509 = null;
                _tpm = null;
                Type = AttestationMechanismType.SymmetricKey;
            }
        }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonPropertyName("tpm")]
        private TpmAttestation Tpm
        {
            get => _tpm;

            set
            {
                _tpm = value;
                _x509 = null;
                _symmetricKey = null;
                Type = AttestationMechanismType.Tpm;
            }
        }

        /// <summary>
        /// Get the attestation of this object. The returned attestation may be of type
        /// <see cref="SymmetricKeyAttestation"/>, <see cref="X509Attestation"/>, or <see cref="TpmAttestation"/>.
        /// By casting the returned Attestation to the appropriate attestation type, you can access the x509/symmetric key
        /// specific attestation fields. Use <see cref="Type"/> to cast this field to the appropriate attestation type.
        /// </summary>
        /// <returns>The attestation of this object.</returns>
        public Attestation GetAttestation()
        {
            return Type switch
            {
                AttestationMechanismType.X509 => _x509,
                AttestationMechanismType.SymmetricKey => _symmetricKey,
                AttestationMechanismType.Tpm => _tpm,
                AttestationMechanismType.None => s_none,
                _ => throw new ProvisioningServiceException("Unknown attestation type", HttpStatusCode.BadRequest),
            };
        }

        private class NoneAttestation : Attestation { }
    }
}