// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt to send a message fails because the length of the message exceeds
    /// the maximum size allowed.
    /// </summary>
    /// <remarks>
    /// When the message is too large for IoT hub you will receive this exception. You should attempt to reduce your
    /// message size and send again. For more information on message sizes, see
    /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits">IoT hub
    /// quotas and throttling | Other limits</see>
    /// </remarks>
    [Serializable]
    public sealed class MessageTooLargeException : IotHubServiceException
    {
        /// <summary>
        /// Creates an instance of this class with the value of the
        /// maximum allowed size of a message in bytes, and marks it as non-transient.
        /// </summary>
        /// <param name="maximumMessageSizeInBytes">The maximum allowed size of the message in bytes.</param>
        public MessageTooLargeException(int maximumMessageSizeInBytes)
            : this(maximumMessageSizeInBytes, string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the value of the
        /// maximum allowed size of a message in bytes and the tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="maximumMessageSizeInBytes">The maximum allowed size of the message in bytes.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public MessageTooLargeException(int maximumMessageSizeInBytes, string trackingId)
            : base($"Message size cannot exceed {maximumMessageSizeInBytes} bytes.", trackingId)
        {
            MaximumMessageSizeInBytes = maximumMessageSizeInBytes;
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MessageTooLargeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public MessageTooLargeException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MessageTooLargeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal MessageTooLargeException()
            : base()
        {
        }

        internal int MaximumMessageSizeInBytes { get; private set; }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MaximumMessageSizeInBytes", MaximumMessageSizeInBytes);
        }
    }
}
