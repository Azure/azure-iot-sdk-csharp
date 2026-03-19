// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Type of Device Provisioning Service attestation mechanism.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AttestationMechanismType
    {
        /// <summary>
        /// None attestation mechanism.
        /// </summary>
        /// <remarks>
        /// There is no valid scenario for `none` Attestation Mechanism Type.
        /// </remarks>
        [EnumMember(Value = "none")]
        None,

        /// <summary>
        /// x509 attestation mechanism.
        /// </summary>
        /// <remarks>
        /// Identify the attestation mechanism as <see cref="X509Attestation"/>.
        /// </remarks>
        [EnumMember(Value = "x509")]
        X509,

        /// <summary>
        /// Symmetric Key attestation mechanism
        /// </summary>
        /// <remarks>
        /// Identify the attestation mechanism as <see cref="SymmetricKeyAttestation"/>.
        /// </remarks>
        [EnumMember(Value = "symmetricKey")]
        SymmetricKey,

        /// <summary>
        /// TPM attestation mechanism
        /// </summary>
        /// <remarks>
        /// Identify the attestation mechanism as <see cref="TpmAttestation"/>.
        /// </remarks>
        [EnumMember(Value = "tpm")]
        Tpm,
    }
}
