// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
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
        private TpmAttestation _tpm;
        private SymmetricKeyAttestation _symmetricKey;

        private static readonly NoneAttestation s_none = new NoneAttestation();

        internal AttestationMechanism(Attestation attestation)
        {
            if (attestation == null)
            {
                throw new ArgumentNullException(nameof(attestation));
            }

            if (attestation is TpmAttestation tpmAttestation)
            {
                Tpm = tpmAttestation;
            }
            else if (attestation is X509Attestation x509Attestation)
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
            else
            {
                throw new ArgumentException("Unknown attestation mechanism");
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
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "tpm", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "x509", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private X509Attestation X509
        {
            get => _x509;

            set
            {
                _x509 = value;
                _tpm = null;
                _symmetricKey = null;
                Type = AttestationMechanismType.X509;
            }
        }

        [JsonProperty(PropertyName = "symmetricKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        /// Get the attestation of this object. The returned attestation may be of type
        /// <see cref="SymmetricKeyAttestation"/>, <see cref="X509Attestation"/>, or <see cref="TpmAttestation"/>.
        /// By casting the returned Attestation to the appropriate attestation type, you can access the x509/symmetric key/tpm
        /// specific attestation fields. Use <see cref="Type"/> to cast this field to the appropriate attestation type.
        /// </summary>
        /// <returns>The attestation of this object.</returns>
        public Attestation GetAttestation()
        {
            return Type switch
            {
                AttestationMechanismType.Tpm => _tpm,
                AttestationMechanismType.X509 => _x509,
                AttestationMechanismType.None => s_none,
                AttestationMechanismType.SymmetricKey => _symmetricKey,
                _ => throw new ProvisioningServiceClientException("Unknown attestation type"),
            };
        }

        private class NoneAttestation : Attestation { }
    }
}
