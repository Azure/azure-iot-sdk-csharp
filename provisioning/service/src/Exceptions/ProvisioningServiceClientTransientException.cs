// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{

    /// <summary>
    /// This is the subset of the Device Provisioning Service exceptions for the exceptions related a temporary service issue.
    /// </summary>
    /// <remarks>
    /// The provisioning service will response a HTTP request with one of the transient exception if
    ///     the rest API was properly called, but the service is not able to execute that action at that
    ///     time. These are the exceptions that a retry can help to fix the issue.
    /// HTTP status code 500 to 599.
    /// </remarks>
    public class ProvisioningServiceClientTransientException : ProvisioningServiceClientHttpException
    {
        public ProvisioningServiceClientTransientException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientTransientException(string registrationId, string trackingId)
            : base("Device Provisioning Service transient error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientTransientException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ProvisioningServiceClientTransientException(ContractApiResponse response)
            : base(response) { }

    }
}
