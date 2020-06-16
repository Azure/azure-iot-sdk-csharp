// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public sealed class QuotaExceededException : IotHubException
    {
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        public QuotaExceededException(ErrorCode code, string message)
            : base(code, message, isTransient: true)
        {
        }

        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        private QuotaExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
