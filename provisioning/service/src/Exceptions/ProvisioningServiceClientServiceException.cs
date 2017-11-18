// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This is the subset of the Device Provisioning Service exceptions for the exceptions reported by the Service. 
    /// </summary>
    public class ProvisioningServiceClientServiceException : ProvisioningServiceClientException
    {
        public ProvisioningServiceClientServiceException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientServiceException(string registrationId, string trackingId)
            : base("Device Provisioning Service error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
