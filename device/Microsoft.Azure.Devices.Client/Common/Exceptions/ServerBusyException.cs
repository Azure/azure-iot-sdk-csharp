// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    using System;
    using System.Runtime.Serialization;

#if !WINDOWS_UWP && !PCL && !NETSTANDARD1_3
    [Serializable]
#endif
#if !PCL
    public
#endif
    sealed class ServerBusyException : IotHubException
    {
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

#if !WINDOWS_UWP && !PCL && !NETSTANDARD1_3
        ServerBusyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
