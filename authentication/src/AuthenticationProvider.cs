// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Authentication
{
    /// <summary>
    /// The authentication provider used by Azure IoT device clients.
    /// </summary>
    public abstract class AuthenticationProvider
    {
        /// <summary>
        /// Gets the registration Id used during device enrollment.
        /// </summary>
        public abstract string GetRegistrationId();
    }
}
