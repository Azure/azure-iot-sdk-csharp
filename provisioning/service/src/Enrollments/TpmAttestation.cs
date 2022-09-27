﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
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
        /// <param name="endorsementKey">the string with the TPM endorsement key. It cannot be null or empty.</param>
        /// <param name="storageRootKey">the string with the TPM storage root key. It can be null or empty.</param>
        /// <exception cref="ArgumentNullException">If the provided endorsementKey is null.</exception>
        /// <exception cref="ArgumentException">If the provided endorsementKey is empty or white space.</exception>
        [JsonConstructor]
        public TpmAttestation(string endorsementKey, string storageRootKey = null)
        {
            try
            {
                Argument.AssertNotNullOrEmpty(endorsementKey, nameof(endorsementKey));
                EndorsementKey = endorsementKey;
                StorageRootKey = storageRootKey;
            }
            catch (ArgumentException e)
            {
                throw new DeviceProvisioningServiceException(e.Message, HttpStatusCode.BadRequest, e);
            }
        }

        /// <summary>
        /// Gets the endorsement key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "endorsementKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EndorsementKey { get; private set; }

        /// <summary>
        /// Gets the storage key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "storageRootKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string StorageRootKey { get; private set; }
    }
}
