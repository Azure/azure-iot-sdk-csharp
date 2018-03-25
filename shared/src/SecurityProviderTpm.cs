// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Provider interface for TPM Hardware Security Modules.
    /// </summary>
    public abstract class SecurityProviderTpm : SecurityProvider
    {
        private readonly string _registrationId;

        /// <summary>
        /// Initializes a new instance of the SecurityProviderTpm class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        public SecurityProviderTpm(string registrationId)
        {
            _registrationId = registrationId;
        }

        /// <summary>
        /// Gets the Registration ID used during device enrollment.
        /// </summary>
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
        /// Activates an identity key within the TPM device.
        /// </summary>
        /// <param name="encryptedKey">The encrypted identity key.</param>
        public abstract void ActivateIdentityKey(byte[] encryptedKey);

        /// <summary>
        /// Signs the data using the previously activated identity key.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public abstract byte[] Sign(byte[] data);
    }
}
