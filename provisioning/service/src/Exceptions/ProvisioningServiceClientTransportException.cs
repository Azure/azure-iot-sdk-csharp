// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create transport exception
    /// </summary>
    public class ProvisioningServiceClientTransportException : ProvisioningServiceClientException
    {
        public ProvisioningServiceClientTransportException()
            : this(message: null, isTransient: true)
        {
        }

        public ProvisioningServiceClientTransportException(string message)
            : this(message, innerException: null)
        {
        }

        public ProvisioningServiceClientTransportException(string message, bool isTransient)
            : base(message, isTransient: isTransient)
        {
        }

        public ProvisioningServiceClientTransportException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }
    }
}
