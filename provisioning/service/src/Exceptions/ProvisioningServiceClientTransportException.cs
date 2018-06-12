// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Transport-level exception during provisioning service operation.
    /// </summary>
    public class ProvisioningServiceClientTransportException : ProvisioningServiceClientException
    {
        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientTransportException"/> class.
        /// </summary>
        public ProvisioningServiceClientTransportException()
            : this(message: null, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientTransportException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ProvisioningServiceClientTransportException(string message)
            : this(message, innerException: null)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientTransportException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="isTransient">True if the operation should be retried.</param>
        public ProvisioningServiceClientTransportException(string message, bool isTransient)
            : base(message, isTransient: isTransient)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientTransportException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningServiceClientTransportException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }
    }
}
