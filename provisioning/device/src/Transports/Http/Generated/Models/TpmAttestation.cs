// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Attestation via TPM.
    /// </summary>
    internal class TpmAttestation
    {
        /// <summary>
        /// Initializes a new instance of the TpmAttestation class.
        /// </summary>
        public TpmAttestation(string endorsementKey = default, string storageRootKey = default)
        {
            EndorsementKey = endorsementKey;
            StorageRootKey = storageRootKey;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "endorsementKey")]
        public string EndorsementKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "storageRootKey")]
        public string StorageRootKey { get; set; }

    }
}
