// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt is made to create a device that already exists in the hub.
    /// </summary>
    [Serializable]
    public sealed class DeviceAlreadyExistsException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="DeviceAlreadyExistsException"/> with the specified device Id, an empty error message
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device that is already registered on IoT hub.</param>
        public DeviceAlreadyExistsException(string deviceId)
            : this(deviceId, string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceAlreadyExistsException"/> with the specified device Id and the service returned tracking Id
        /// associated with this particular error, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device that is already registered on IoT hub.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public DeviceAlreadyExistsException(string deviceId, string trackingId)
            : base($"Device {deviceId} already registered.", trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceAlreadyExistsException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceAlreadyExistsException"/> with a specified <see cref="ErrorCode"/>, error message and an
        /// optional reference to the inner exception that caused this exception. This exception is marked as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceAlreadyExistsException(ErrorCode code, string message, Exception innerException = null)
            : base(code, message, innerException)
        {
        }

        internal DeviceAlreadyExistsException()
            : base()
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceAlreadyExistsException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [SuppressMessage("Usage", "CA2229:Implement serialization constructors",
            Justification = "Cannot modify public API surface since it will be a breaking change")]
        public DeviceAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
