// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the rate of incoming requests exceeds the throttling limit set by IoT hub.
    /// </summary>
    [Serializable]
    public class ThrottlingException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="ThrottlingException"/> with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ThrottlingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ThrottlingException"/> with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public ThrottlingException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ThrottlingException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public ThrottlingException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ThrottlingException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ThrottlingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal ThrottlingException()
            : base()
        {
        }
    }
}
