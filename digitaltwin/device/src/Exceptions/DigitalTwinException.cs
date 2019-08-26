// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Azure.Iot.DigitalTwin.Device.Exceptions
{
    /// <summary>
    /// base exception for Digital Twin.
    /// </summary>
    public abstract class DigitalTwinException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinException"/> class.
        /// </summary>
        public DigitalTwinException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DigitalTwinException(string message)
                : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DigitalTwinException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="streamingContext">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DigitalTwinException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
