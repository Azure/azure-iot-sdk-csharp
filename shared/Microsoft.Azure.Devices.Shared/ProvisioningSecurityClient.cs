// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Security Client used during device provisioning and IoT Hub hardware-based authentication.
    /// </summary>
    public abstract class ProvisioningSecurityClient : IDisposable
    {
        /// <summary>
        /// The Registration ID used during device enrollment.
        /// </summary>
        public string RegistrationID { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the SecurityClientSasToken class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        public ProvisioningSecurityClient(string registrationId) 
        {
            RegistrationID = registrationId;
        }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProvisioningSecurityClient()
        {
            Dispose(false);
        }
    }
}
