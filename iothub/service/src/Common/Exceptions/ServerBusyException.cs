// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public sealed class ServerBusyException : IotHubException
    {
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        public ServerBusyException(ErrorCode code, string message)
            : base(code, message, true)
        {
        }

        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        private ServerBusyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
