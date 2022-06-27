// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Attestation via TPM.
    /// </summary>
    internal partial class TpmAttestation
    {
        /// <summary>
        /// Initializes a new instance of the TpmAttestation class.
        /// </summary>
        public TpmAttestation()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TpmAttestation class.
        /// </summary>
        public TpmAttestation(string endorsementKey = default(string), string storageRootKey = default(string))
        {
            EndorsementKey = endorsementKey;
            StorageRootKey = storageRootKey;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

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
