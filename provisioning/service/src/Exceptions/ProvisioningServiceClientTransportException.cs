// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Create transport exception
    /// </summary>
#if !WINDOWS_UWP
    [Serializable]
#endif
    public sealed class ProvisioningServiceClientTransportException : ProvisioningServiceClientException
    {
        public ProvisioningServiceClientTransportException(string message)
            : this(message, null)
        {
        }

        public ProvisioningServiceClientTransportException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

//#if !WINDOWS_UWP && !NETSTANDARD1_3
//        ProvisioningServiceClientTransportException(SerializationInfo info, StreamingContext context)
//            : base(info, context)
//        {
//        }
//#endif
    }
}
