// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Authentication
{
    // Commented out until we resolve plans for TPM support and library dependency
    ///// <summary>
    ///// The device authentication provider interface for TPM hardware security modules.
    ///// </summary>
    //public abstract class AuthenticationProviderTpm : AuthenticationProvider
    //{
    //    private readonly string _registrationId;

    //    /// <summary>
    //    /// Creates an instance of this class.
    //    /// </summary>
    //    /// <param name="registrationId">The Provisioning service Registration Id for this device.</param>
    //    public AuthenticationProviderTpm(string registrationId)
    //    {
    //        _registrationId = registrationId;
    //    }

    //    /// <inheritdoc/>
    //    public override string GetRegistrationId()
    //    {
    //        return _registrationId;
    //    }

    //    /// <summary>
    //    /// Gets the Base64 encoded EndorsementKey.
    //    /// </summary>
    //    /// <returns>Base64 encoded EK.</returns>
    //    public abstract byte[] GetEndorsementKey();

    //    /// <summary>
    //    /// Gets the Base64 encoded StorageRootKey.
    //    /// </summary>
    //    /// <returns>Base64 encoded SRK.</returns>
    //    public abstract byte[] GetStorageRootKey();

    //    /// <summary>
    //    /// Activates an identity key within the TPM device.
    //    /// </summary>
    //    /// <param name="encryptedKey">The encrypted identity key.</param>
    //    public abstract void ActivateIdentityKey(byte[] encryptedKey);

    //    /// <summary>
    //    /// Signs the data using the previously activated identity key.
    //    /// </summary>
    //    /// <param name="data">The data to be signed.</param>
    //    /// <returns>The signed data.</returns>
    //    public abstract byte[] Sign(byte[] data);
    //}
}
