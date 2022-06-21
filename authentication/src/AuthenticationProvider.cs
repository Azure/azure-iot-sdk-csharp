// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Authentication
{
    /// <summary>
    /// The authentication provider used by Azure IoT device clients.
    /// </summary>
    public abstract class AuthenticationProvider : IDisposable
    {
        /// <summary>
        /// Gets the Registration Id used during device enrollment.
        /// </summary>
        public abstract string GetRegistrationID();

        /// <summary>
        /// Releases the unmanaged resources used by this class and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the invoker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
