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
        /// For deserialization and unit testing.
        /// </summary>
        /// <param name="primaryKey">The primary key to use for attestation</param>
        /// <param name="secondaryKey">The secondary key to use for attestation</param>
        [JsonConstructor]
        public SymmetricKeyAttestation(string primaryKey, string secondaryKey)
        {
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey;
        }

        /// <summary>
        /// Gets the primary key used for attestation.
        /// </summary>
        [JsonPropertyName("primaryKey")]
        public string PrimaryKey { get; }

        /// <summary>
        /// Gets the secondary key used for attestation.
        /// </summary>
        [JsonPropertyName("secondaryKey")]
        public string SecondaryKey { get; }
    }
}