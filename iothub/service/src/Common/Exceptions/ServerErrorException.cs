// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT Hub encounters an error while processing a request.
    /// </summary>
    [Serializable]
    public sealed class ServerErrorException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="ServerErrorException"/> with a specified error message and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServerErrorException(string message)
            : base(message, isTransient: true)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerErrorException"/> with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public ServerErrorException(ErrorCode code, string message)
            : base(code, message, isTransient: true)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerErrorException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ServerErrorException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        internal ServerErrorException()
            : base()
        {
        }

        private ServerErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
