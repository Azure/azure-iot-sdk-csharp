// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create Device Provisioning Service not found exception.
    /// </summary>
    /// <remarks>
    /// The Device Provisioning Service instance, or a resource (e.g. an enrollment) does not exist.
    /// HTTP status code 404.
    /// </remarks>
    public class ProvisioningServiceClientNotFoundException : ProvisioningServiceClientBadUsageException
    {
        public ProvisioningServiceClientNotFoundException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientNotFoundException(string registrationId, string trackingId)
            : base("Device Provisioning Service not found for " + registrationId, trackingId) { }

        public ProvisioningServiceClientNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
