// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create internal server error exception.
    /// </summary>
    /// <remarks>
    /// An internal error occurred.
    /// HTTP status code 500.
    /// </remarks>
    public class ProvisioningServiceClientInternalServerErrorException : ProvisioningServiceClientTransientException
    {
        public ProvisioningServiceClientInternalServerErrorException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientInternalServerErrorException(string registrationId, string trackingId)
            : base("Internal server error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientInternalServerErrorException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
