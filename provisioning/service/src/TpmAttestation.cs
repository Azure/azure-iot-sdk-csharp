// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Used to describe the TPM attestation mechanism.
    /// </summary>
    public sealed class TpmAttestation
    {
        /// <summary>
        /// Creates a new instance of <see cref="TpmAttestation"/>
        /// </summary>
        public TpmAttestation()
        {
        }

        /// <summary>
        /// Gets or sets the endorsement key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "endorsementKey")]
        public string EndorsementKey { get; set; }

        /// <summary>
        /// Gets or sets the storage key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "storageRootKey")]
        public string StorageRootKey { get; set; }
    }
}
