// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{

    /// <summary>
    /// Create precondition failed exception.
    /// </summary>
    /// <remarks>
    /// The ETag in the request does not match the ETag of the existing resource, as per RFC7232.
    /// HTTP status code 412.
    /// </remarks>
    public class ProvisioningServiceClientPreconditionFailedException : ProvisioningServiceClientBadUsageException
    {
        public ProvisioningServiceClientPreconditionFailedException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientPreconditionFailedException(string registrationId, string trackingId)
            : base("Precondition failed for " + registrationId, trackingId) { }

        public ProvisioningServiceClientPreconditionFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
