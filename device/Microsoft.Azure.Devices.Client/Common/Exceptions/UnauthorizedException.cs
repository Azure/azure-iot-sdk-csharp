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
    sealed class UnauthorizedException : IotHubException
    {
        public UnauthorizedException(string message)
            : this(message, null)
        {
        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

#if !WINDOWS_UWP && !PCL && !NETSTANDARD1_3
        UnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}

