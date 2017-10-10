// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for SAS Token authentication (TPM authentication).
    /// </summary>
    public abstract class ProvisioningSecurityClientSasToken : ProvisioningSecurityClient
    {
        /// <summary>
        /// Initializes a new instance of the SecuritySasToken class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        public ProvisioningSecurityClientSasToken(string registrationId) : base(registrationId)
        {
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsementKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public abstract Task<byte[]> GetEndorsementKeyAsync();

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public abstract Task<byte[]> GetStorageRootKeyAsync();

        /// <summary>
        /// Activates a symmetric identity within the Hardware Security Module.
        /// </summary>
        /// <param name="activation">The authentication challenge key supplied by the service.</param>
        public abstract Task ActivateSymmetricIdentityAsync(byte[] activation);

        /// <summary>
        /// Signs the data using the Hardware Security Module.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public abstract Task<byte[]> SignAsync(byte[] data);
    }
}
