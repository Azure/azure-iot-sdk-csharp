﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Provider interface for Symmetric Keys.
    /// </summary>
    public class SecurityProviderSymmetricKey : SecurityProvider
    {
        private readonly string _registrationId;
        private readonly string _primaryKey;
        private readonly string _secondaryKey;

        /// <summary>
        /// Initializes a new instance of the SecurityProviderSymmetricKey class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration Id for this device.</param>
        /// <param name="primaryKey">The primary key for this device.</param>
        /// <param name="secondaryKey">The secondary key for this device.</param>
        public SecurityProviderSymmetricKey(string registrationId, string primaryKey, string secondaryKey)
        {
            _registrationId = registrationId;
            _primaryKey = primaryKey;
            _secondaryKey = secondaryKey;
        }

        /// <summary>
        /// Gets the Registration Id used during device enrollment.
        /// </summary>
        public override string GetRegistrationID()
        {
            return _registrationId;
        }

        /// <summary>
        /// Gets the primary key.
        /// </summary>
        /// <returns>primary key</returns>
        public string GetPrimaryKey() { return _primaryKey; }

        /// <summary>
        /// Gets the secondary key.
        /// </summary>
        /// <returns>secondary key</returns>
        public string GetSecondaryKey() { return _secondaryKey; }

        /// <summary>
        /// Releases all resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected override void Dispose(bool disposing) { }
    }
}
