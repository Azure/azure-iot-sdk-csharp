// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Security Provider used by ProvisioningDeviceClient for authentication.
    /// </summary>
    public abstract class SecurityProvider : IDisposable
    {
        /// <summary>
        /// Gets the Registration ID used during device enrollment.
        /// </summary>
        public abstract string GetRegistrationID();

        /// <summary>
        /// Releases the unmanaged resources used by the SecurityProvider and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the invoker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
