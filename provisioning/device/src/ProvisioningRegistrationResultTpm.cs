// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The ProvisioningRegistrationResult type returned when the SAS Token HSM mode is used.
    /// </summary>
    public class ProvisioningRegistrationResultTpm : ProvisioningRegistrationResult
    {
        /// <summary>
        /// The AuthenticationKey required by the SAS Token HSM module.
        /// </summary>
        public string AuthenticationKey { get; private set; }

        public ProvisioningRegistrationResultTpm(
            string registrationId,
            DateTime? createdDateTimeUtc,
            string assignedHub,
            string deviceId,
            ProvisioningRegistrationStatusType status,
            string generationId,
            DateTime? lastUpdatedDateTimeUtc,
            int errorCode,
            string errorMessage,
            string etag,
            string authenticationKey) : base(
                registrationId, 
                createdDateTimeUtc,
                assignedHub,
                deviceId,
                status,
                generationId,
                lastUpdatedDateTimeUtc,
                errorCode,
                errorMessage,
                etag)
        {
            AuthenticationKey = authenticationKey;
        }
    }
}
