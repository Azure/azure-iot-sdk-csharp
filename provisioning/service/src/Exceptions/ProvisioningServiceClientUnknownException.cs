// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This is the subset of the Device Provisioning Service exceptions for the unknown issues.
    /// </summary>
    /// <remarks>
    /// <p> HTTP status code 300+, but not 4nn or 5nn.
    /// </remarks>
    public class ProvisioningServiceClientUnknownException : ProvisioningServiceClientHttpException
    {
        public ProvisioningServiceClientUnknownException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientUnknownException(string registrationId, string trackingId)
            : base("Device Provisioning Service unknown error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientUnknownException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
