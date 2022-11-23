// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service TPM Attestation.
    /// </summary>
    /// <remarks>
    /// The provisioning service supports Trusted Platform Module, or TPM, as the device attestation mechanism.
    /// User must provide the Endorsement Key, and can, optionally, provide the Storage Root Key.
    /// </remarks>
    public class TpmAttestation : Attestation
    {
        /// <summary>
        /// Gets the endorsement key used for attestation.
        /// </summary>
        [JsonPropertyName("endorsementKey")]
        public string EndorsementKey { get; }

        /// <summary>
        /// Gets the storage key used for attestation.
        /// </summary>
        [JsonPropertyName("storageRootKey")]
        public string StorageRootKey { get; }
    }
}
