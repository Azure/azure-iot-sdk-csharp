// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The DPS Security Client implementation for TPM.
    /// </summary>
    public class SecurityClientTpm : SecurityClientSasToken
    {
        Tpm2Device _tpm;

        /// <summary>
        /// Constructor creating an instance using the system TPM.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        public SecurityClientTpm(string registrationId) : base(registrationId)
        {
            _tpm = null; // TODO: what would be a good X-Plat default?
        }

        /// <summary>
        /// Constructor creating an instance using the specified TPM module.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        /// <param name="tpm">The TPM device.</param>
        public SecurityClientTpm(string registrationId, Tpm2Device tpm) : base(registrationId)
        {
            _tpm = tpm;
        }

        /// <summary>
        /// Activates a symmetric identity within the Hardware Security Module.
        /// </summary>
        /// <param name="activation">The authentication challenge key supplied by the service.</param>
        public override void ActivateSymmetricIdentity(byte[] activation)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsmentKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public override string GetEndorsementKey()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public override string GetStorageRootKey()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Signs the data using the Hardware Security Module.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public override byte[] Sign(byte[] data)
        {
            throw new System.NotImplementedException();
        }
    }
}
