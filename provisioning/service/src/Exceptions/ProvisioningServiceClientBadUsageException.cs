// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This is the subset of the Device Provisioning Service exceptions for the exceptions related to usage error.
    /// </summary>
    /// <remarks>
    /// The provisioning service will response a HTTP request with one of the bad usage exception if
    ///    the rest API was properly called, with a usage issue, for instance the user is not authorized
    ///    for that operation.
    /// HTTP status code 400 to 499.
    /// </remarks>
    public class ProvisioningServiceClientBadUsageException : ProvisioningServiceClientHttpException
    {
        public ProvisioningServiceClientBadUsageException(string registrationId)
            : this(registrationId, string.Empty) { }

        public ProvisioningServiceClientBadUsageException(string registrationId, string trackingId)
            : base("Bad usage for " + registrationId, trackingId) { }

        public ProvisioningServiceClientBadUsageException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ProvisioningServiceClientBadUsageException(ContractApiResponse response)
            : base(response) { }
    }
}
