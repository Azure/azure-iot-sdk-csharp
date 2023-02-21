// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The device authentication provider interface for symmetric keys.
    /// </summary>
    public class AuthenticationProviderSymmetricKey : AuthenticationProvider
    {
        private readonly string _registrationId;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration Id for this device.</param>
        /// <param name="primaryKey">The primary key for this device.</param>
        /// <param name="secondaryKey">The secondary key for this device.</param>
        /// <exception cref="ArgumentException">Thrown when the required parameter is an empty string or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the required parameter is null.</exception>
        public AuthenticationProviderSymmetricKey(string registrationId, string primaryKey, string secondaryKey)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));
            Argument.AssertNotNullOrWhiteSpace(primaryKey, nameof(primaryKey));
            Argument.AssertNotNullOrWhiteSpace(secondaryKey, nameof(secondaryKey));

            _registrationId = registrationId;
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey;
        }

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected internal AuthenticationProviderSymmetricKey()
        { }

        /// <summary>
        /// The primary key for this device.
        /// </summary>
        public string PrimaryKey { get; }

        /// <summary>
        /// The secondary key for this device.
        /// </summary>
        public string SecondaryKey { get; }

        /// <inheritdoc/>
        public override string GetRegistrationId()
        {
            return _registrationId;
        }
    }
}
