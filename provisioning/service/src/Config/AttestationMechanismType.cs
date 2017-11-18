// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Type of Device Provisioning Service attestation mechanism.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AttestationMechanismType
    {
        /// <summary>
        /// None attestation mechanism.
        /// </summary>
        /// <remarks>
        /// There is no valid scenario for `none` Attestation Mechanism Type.
        /// </remarks>
        [JsonProperty(PropertyName = "none")]
        None,

        /// <summary>
        /// Tpm attestation mechanism.
        /// </summary>
        /// <remarks>
        /// Identify the attestation mechanism as <see cref="TpmAttestation"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "tpm")]
        Tpm = 1,

        /// <summary>
        /// x509 attestation mechanism.
        /// </summary>
        /// <remarks>
        /// Identify the attestation mechanism as <see cref="X509Attestation"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "x509")]
        X509 = 2
    }
}
