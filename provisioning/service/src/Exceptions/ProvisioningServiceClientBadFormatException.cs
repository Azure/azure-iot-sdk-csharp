// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create bad message format exception
    /// </summary>
    /// <remarks>
    /// The body of the HTTP request is not valid; for example, it cannot be parsed, or the object cannot be validated.
    /// HTTP status code 400.
    /// </remarks>
    public class ProvisioningServiceClientBadFormatException : ProvisioningServiceClientBadUsageException
    {
        public ProvisioningServiceClientBadFormatException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientBadFormatException(string registrationId, string trackingId)
            : base("Bad message format for " + registrationId, trackingId) { }

        public ProvisioningServiceClientBadFormatException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
