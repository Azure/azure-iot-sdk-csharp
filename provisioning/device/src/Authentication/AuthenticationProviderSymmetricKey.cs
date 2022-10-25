// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The device authentication provider interface for symmetric keys.
    /// </summary>
    public class AuthenticationProviderSymmetricKey : AuthenticationProvider
    {
        private readonly string _registrationId;
        private readonly string _primaryKey;
        private readonly string _secondaryKey;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration Id for this device.</param>
        /// <param name="primaryKey">The primary key for this device.</param>
        /// <param name="secondaryKey">The secondary key for this device.</param>
        public AuthenticationProviderSymmetricKey(string registrationId, string primaryKey, string secondaryKey)
        {
            _registrationId = registrationId;
            _primaryKey = primaryKey;
            _secondaryKey = secondaryKey;
        }

        /// <inheritdoc/>
        public override string GetRegistrationId()
        {
            return _registrationId;
        }

        /// <summary>
        /// Gets the primary key.
        /// </summary>
        /// <returns>primary key</returns>
        public string GetPrimaryKey() => _primaryKey;

        /// <summary>
        /// Gets the secondary key.
        /// </summary>
        /// <returns>secondary key</returns>
        public string GetSecondaryKey() => _secondaryKey;
    }
}
