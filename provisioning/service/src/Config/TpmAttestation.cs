// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service TPM Attestation.
    /// </summary>
    /// <remarks>
    /// The provisioning service supports Trusted Platform Module, or TPM, as the device attestation mechanism.
    /// User must provide the Endorsement Key, and can, optionally, provide the Storage Root Key.
    /// </remarks>
    ///
    public sealed class TpmAttestation : Attestation
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// This function will create a new instance of the TPM attestation
        /// with both endorsement and storage root keys. Only the endorsement
        /// key is mandatory.
        /// </remarks>
        ///
        /// <param name="endorsementKey">the <code>string</code> with the TPM endorsement key. It cannot be <code>null</code> or empty.</param>
        /// <param name="storageRootKey">the <code>string</code> with the TPM storage root key. It can be <code>null</code> or empty.</param>
        /// <exception cref="ArgumentException">if the endorsementKey is <code>null</code> or empty.</exception>
        [JsonConstructor]
        public TpmAttestation(string endorsementKey, string storageRootKey = null)
        {
            /* SRS_TPM_ATTESTATION_21_003: [The constructor shall store the provided endorsementKey and storageRootKey.] */
            /* SRS_TPM_ATTESTATION_21_004: [The TpmAttestation shall provide means to serialization and deserialization.] */
            try
            {
                EndorsementKey = endorsementKey;
                StorageRootKey = storageRootKey;
            }
            catch (ArgumentException e)
            {
                throw new ProvisioningServiceClientException(e);
            }
        }

        /// <summary>
        /// Gets the endorsement key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "endorsementKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EndorsementKey
        {
            get
            {
                return _endorsementKey;
            }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    /* SRS_TPM_ATTESTATION_21_001: [The EndorsementKey setter shall throws ArgumentNullException if the provided 
                                                endorsementKey is null or white space.] */
                    throw new ArgumentNullException(nameof(value));
                }
                _endorsementKey = value;
            }
        }
        private string _endorsementKey;

        /// <summary>
        /// Gets the storage key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "storageRootKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string StorageRootKey
        {
            get
            {
                return _storageRootKey;
            }
            private set
            {
                /* SRS_TPM_ATTESTATION_21_002: [The StorageRootKey setter shall store the storageRootKey passed.] */
                _storageRootKey = value;
            }
        }
        private string _storageRootKey;
    }
}
