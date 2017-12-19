// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Super class for the Device Provisioning Service exceptions on the Service Client.
    /// </summary>
    /// <remarks>
    /// <code>
    /// ProvisioningServiceClientException
    ///    |        \__IsTransient [identify if retry is a valid scenario]
    ///    |
    ///    +-->ProvisioningServiceClientTransportException [any transport layer exception]
    ///    |
    ///    +-->ProvisioningServiceClientHttpException [any exception reported in the HTTP response]
    /// </code>
    /// </remarks>
    public class ProvisioningServiceClientException : Exception
    {
        public bool IsTransient { get; private set; }

        public ProvisioningServiceClientException(string message)
            : this(message, innerException: null, isTransient: false)
        {
        }

        public ProvisioningServiceClientException(string message, bool isTransient)
            : this(message, innerException: null, isTransient: isTransient)
        {
        }

        public ProvisioningServiceClientException(Exception innerException)
            : this(string.Empty, innerException, isTransient: false)
        {
        }

        public ProvisioningServiceClientException(string message, Exception innerException)
            : this(message, innerException, isTransient: false)
        {
        }

        protected ProvisioningServiceClientException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }
    }
}
