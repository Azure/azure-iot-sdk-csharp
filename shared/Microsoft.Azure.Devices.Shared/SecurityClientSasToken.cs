// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for SAS Token authentication (TPM authentication).
    /// </summary>
    public abstract class SecurityClientSasToken : SecurityClient
    {
        /// <summary>
        /// The Registration ID used during device enrollment.
        /// </summary>
        public string RegistrationID { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the SecurityClientSasToken class.
        /// </summary>
        /// <param name="registrationId"></param>
        public SecurityClientSasToken(string registrationId)
        {
            RegistrationID = registrationId;
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsmentKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public abstract string GetEndorsmentKey();

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public abstract string GetStorageRootKey();

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
