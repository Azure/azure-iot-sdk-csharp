// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create unauthorized exception.
    /// </summary>
    /// <remarks>
    /// <p> The authorization token cannot be validated; for example, it is expired or does not apply to the
    ///     requested URI. This error code is also returned to devices as part of the TPM attestation flow.
    /// <p> HTTP status code 401
    /// </remarks>
    public class ProvisioningServiceClientUnathorizedException : ProvisioningServiceClientBadUsageException
    {
        public ProvisioningServiceClientUnathorizedException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientUnathorizedException(string registrationId, string trackingId)
            : base("Unauthorized for " + registrationId, trackingId) { }

        public ProvisioningServiceClientUnathorizedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
