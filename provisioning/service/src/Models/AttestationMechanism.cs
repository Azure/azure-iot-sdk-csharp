// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

            switch (attestation)
            {
                case X509Attestation x509Attestation:
                    X509 = x509Attestation;
                    break;
                case NoneAttestation:
                    // No-op.
                    break;
                case SymmetricKeyAttestation symmetricKeyAttestation:
                    SymmetricKey = symmetricKeyAttestation;
                    break;
                case TpmAttestation tpmAttestion:
                    Tpm = tpmAttestion;
                    break;
                default:
                    throw new ArgumentException("Unknown attestation mechanism");
            }
        }

        [JsonConstructor]
#pragma warning disable IDE0051 // Used for serialization
        private AttestationMechanism(AttestationMechanismType type, X509Attestation x509, TpmAttestation tpm, SymmetricKeyAttestation symmetricKey)
#pragma warning restore IDE0051
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
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public AttestationMechanismType Type { get; set; }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonProperty("x509", DefaultValueHandling = DefaultValueHandling.Ignore)]
#pragma warning disable IDE0052 // Used for serialization
        private X509Attestation X509
        {
            get => _x509;
#pragma warning restore IDE0052

            set
            {
                _x509 = value;
                _symmetricKey = null;
                _tpm = null;
                Type = AttestationMechanismType.X509;
            }
        }

        [JsonProperty("symmetricKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
#pragma warning disable IDE0052 // Used for serialization
        private SymmetricKeyAttestation SymmetricKey
        {
            get => _symmetricKey;
#pragma warning restore IDE0052

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
        [JsonProperty("tpm", DefaultValueHandling = DefaultValueHandling.Ignore)]
#pragma warning disable IDE0052 // Used for serialization
        private TpmAttestation Tpm
        {
            get => _tpm;
#pragma warning restore IDE0052

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
