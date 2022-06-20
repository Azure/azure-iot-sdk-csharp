// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Super class for the Device Provisioning Service exceptions on the Service Client.
    /// </summary>
    /// <remarks>
    /// <c>
    /// ProvisioningServiceClientException
    ///    |        \__IsTransient [identify if retry is a valid scenario]
    ///    |
    ///    +-->ProvisioningServiceClientTransportException [any transport layer exception]
    ///         |
    ///         +-->ProvisioningServiceClientHttpException [any exception reported in the HTTP response]
    /// </c>
    /// </remarks>
    public class ProvisioningServiceClientException : Exception
    {
        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        public ProvisioningServiceClientException()
            : this(message: null, innerException: null, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        public ProvisioningServiceClientException(string message)
            : this(message, innerException: null, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        public ProvisioningServiceClientException(string message, bool isTransient)
            : this(message, innerException: null, isTransient: isTransient)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningServiceClientException(Exception innerException)
            : this(string.Empty, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        public ProvisioningServiceClientException(string message, Exception innerException)
            : this(message, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientException"/> exception type.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        protected ProvisioningServiceClientException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// True if the error is transient.
        /// </summary>
        public bool IsTransient { get; private set; }
    }
}
