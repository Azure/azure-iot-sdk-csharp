// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The DPSRegistrationResult type returned when the SAS Token HSM mode is used.
    /// </summary>
    public class ProvisioningRegistrationResultSasToken : ProvisioningRegistrationResult
    {
        /// <summary>
        /// The Registration ID used during device enrollment.
        /// </summary>
        public string RegistrationID { get; protected set; }

        /// <summary>
        /// The AuthenticationKey required by the SAS Token HSM module.
        /// </summary>
        public string AuthenticationKey { get; private set; }

        public ProvisioningRegistrationResultSasToken(string registrationId)
        {
            RegistrationId = registrationId;
        }
    }
}
