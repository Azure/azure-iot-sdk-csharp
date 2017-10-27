// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Security Client used during device provisioning and IoT Hub hardware-based authentication.
    /// </summary>
    public abstract class SecurityClient : IDisposable
    {
        /// <summary>
        /// Gets the Registration ID used during device enrollment.
        /// </summary>
        public abstract string GetRegistrationID();

        protected abstract void Dispose(bool disposing);
   
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
