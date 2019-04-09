// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// The Device Security Provider interface for Symmetric Keys
    /// </summary>
    public class SecurityProviderSymmetricKey : SecurityProvider
    {
        private readonly string _registrationId;
        private readonly string _primaryKey;
        private readonly string _secondaryKey;

        /// <summary>
        /// Initializes a new instance of the SecurityProviderSymmetricKey class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        /// /// <param name="primaryKey">The primary symmetric key.</param>
        /// /// <param name="secondaryKey">The secondary symmetric key.</param>
        public SecurityProviderSymmetricKey(string registrationId, string primaryKey, string secondaryKey)
        {
            _registrationId = registrationId;
            _primaryKey = primaryKey;
            _secondaryKey = secondaryKey;
        }

        /// <summary>
        /// Gets the Registration ID used during device enrollment.
        /// </summary>
        public override string GetRegistrationID()
        {
            return _registrationId;
        }

        /// <summary>
        /// Gets the Base64 encoded primary key.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public string GetPrimaryKey() { return _primaryKey; }

        /// <summary>
        /// Gets the Secondary key
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public string GetSecondaryKey() { return _secondaryKey; }

        /// <summary>
        /// Releases the unmanaged resources used by the SecurityProviderX509Certificate and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected override void Dispose(bool disposing) { }
    }
}
