// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Attestation using a symmetric key.
    /// </summary>
    public sealed class SymmetricKeyAttestation : Attestation
    {
        /// <summary>
        /// Gets the primary key used for attestation.
        /// </summary>
        [JsonPropertyName("primaryKey")]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Gets the secondary key used for attestation.
        /// </summary>
        [JsonPropertyName("secondaryKey")]
        public string SecondaryKey { get; set; }
    }
}