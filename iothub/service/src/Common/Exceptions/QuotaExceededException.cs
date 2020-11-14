// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the allocated quota set by IoT Hub is exceeded.
    /// </summary>
    [Serializable]
    public sealed class QuotaExceededException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="QuotaExceededException"/> with a specified error message and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="QuotaExceededException"/> with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public QuotaExceededException(ErrorCode code, string message)
            : base(code, message, isTransient: true)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="QuotaExceededException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        internal QuotaExceededException()
            : base()
        {
        }

        private QuotaExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
