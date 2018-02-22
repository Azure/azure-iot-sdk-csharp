// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class ServerBusyException : IotHubException
    {
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

#if !NETSTANDARD1_3
        ServerBusyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
