// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// This exception is not directly returned by the service for ServiceClient operations. However, the status code
    /// HttpStatusCode.PreconditionFailed is converted to this exception.
    /// </summary>
    [Serializable]
    public class DeviceMessageLockLostException : IotHubServiceException
    {
        /// <summary>
        /// Creates an instance of <see cref="DeviceMessageLockLostException"/> with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceMessageLockLostException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMessageLockLostException"/> with a specified <see cref="IotHubStatusCode"/>
        /// and error message, and marks it as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="IotHubStatusCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public DeviceMessageLockLostException(IotHubStatusCode code, string message)
            : base(code, message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMessageLockLostException"/> with the device Id and message Id,
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device to which the message was sent.</param>
        /// <param name="messageId">The Id of the message that was sent.</param>
        public DeviceMessageLockLostException(string deviceId, Guid messageId)
            : this(deviceId, messageId, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMessageLockLostException"/> with the device Id, message Id
        /// and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device to which the message was sent.</param>
        /// <param name="messageId">The Id of the message that was sent.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public DeviceMessageLockLostException(string deviceId, Guid messageId, string trackingId)
            : base($"Message {messageId} lock was lost for Device {deviceId}.", trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMessageLockLostException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected DeviceMessageLockLostException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal DeviceMessageLockLostException()
            : base()
        {
        }

        internal DeviceMessageLockLostException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
