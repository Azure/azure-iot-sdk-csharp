// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{

    /// <summary>
    /// Create too many requests exception.
    /// </summary>
    /// <remarks>
    /// Operations are being throttled by the service. For specific service limits, see IoT Hub Device Provisioning
    ///     Service limits.
    /// HTTP status code 429.
    /// </remarks>
    public class ProvisioningServiceClientTooManyRequestsException : ProvisioningServiceClientBadUsageException
    {
        public ProvisioningServiceClientTooManyRequestsException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientTooManyRequestsException(string registrationId, string trackingId)
            : base("Device Provisioning Service error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientTooManyRequestsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
