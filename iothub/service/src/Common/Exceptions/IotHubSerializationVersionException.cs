// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT hub receives an invalid serialization version number.
    /// Note: This exception is currently not thrown by the client library.
    /// </summary>
    [Serializable]
    public class IotHubSerializationVersionException : IotHubSerializationException
    {
        /// <summary>
        /// Creates an instance of this class with the specified serialization version number
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="receivedVersion">The serialization version number.</param>
        public IotHubSerializationVersionException(int receivedVersion)
            : this($"Unrecognized Serialization Version: {receivedVersion}")
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubSerializationVersionException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IotHubSerializationVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal IotHubSerializationVersionException()
            : base()
        {
        }

        internal IotHubSerializationVersionException(string message)
            : base(message)
        {
        }

        internal IotHubSerializationVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
