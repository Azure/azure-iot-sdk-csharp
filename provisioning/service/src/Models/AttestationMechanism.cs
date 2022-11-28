// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service Attestation mechanism of an IndividualEnrollment or an EnrollmentGroup.
    /// </summary>
    public sealed class AttestationMechanism
    {
        /// <summary>
        ///  Attestation Type.
        /// </summary>
        [JsonPropertyName("type")]
        public AttestationMechanismType Type { get; set; }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonPropertyName("x509")]
        public X509Attestation X509 { get; set; }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonPropertyName("symmetricKey")]
        public SymmetricKeyAttestation SymmetricKey { get; set; }

        /// <summary>
        /// Gets or sets the instance used for attestation.
        /// </summary>
        [JsonPropertyName("tpm")]
        public TpmAttestation Tpm { get; set; }
    }
}
