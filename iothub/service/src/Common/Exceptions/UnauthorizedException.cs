// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public sealed class UnauthorizedException : IotHubException
    {
        public UnauthorizedException(string message)
            : this(message, null)
        {
        }

        public UnauthorizedException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

        private UnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
