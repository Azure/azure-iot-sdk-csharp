// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for TPM authentication.
    /// </summary>
    public abstract class SecurityClientHsmTpm : SecurityClient
    {
        private string _registrationId;

        /// <summary>
        /// Initializes a new instance of the SecurityClientHsmTpm class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        public SecurityClientHsmTpm(string registrationId)
        {
            _registrationId = registrationId;
        }

        public override string GetRegistrationID()
        {
            return _registrationId;
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsementKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public abstract byte[] GetEndorsementKey();

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public abstract byte[] GetStorageRootKey();

        /// <summary>
        /// Activates a symmetric identity within the Hardware Security Module.
        /// </summary>
        /// <param name="activation">The authentication challenge key supplied by the service.</param>
        public abstract void ActivateSymmetricIdentity(byte[] activation);

        /// <summary>
        /// Signs the data using the Hardware Security Module.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public abstract byte[] Sign(byte[] data);
    }
}
