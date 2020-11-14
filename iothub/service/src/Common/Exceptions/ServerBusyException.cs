// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT Hub is busy with previous requests.
    /// Callers should wait a while and retry the operation.
    /// </summary>
    [Serializable]
    public sealed class ServerBusyException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified error message and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public ServerBusyException(ErrorCode code, string message)
            : base(code, message, true)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        internal ServerBusyException()
            : base()
        {
        }

        private ServerBusyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
