// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT hub receives an invalid serialization request.
    /// </summary>
    [Serializable]
    public class IotHubSerializationException : IotHubServiceException
    {
        /// <summary>
        /// Creates an instance of this class with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IotHubSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubSerializationException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IotHubSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal IotHubSerializationException()
            : base()
        {
        }

        internal IotHubSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
