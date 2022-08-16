// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt to enqueue a message fails because the message queue for the device is already full.
    /// </summary>
    [Serializable]
    public sealed class DeviceMaximumQueueDepthExceededException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="DeviceMaximumQueueDepthExceededException"/> with the specified value of the maximum queue depth
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="maximumQueueDepth"></param>
        public DeviceMaximumQueueDepthExceededException(int maximumQueueDepth)
            : this(maximumQueueDepth, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMaximumQueueDepthExceededException"/> with the specified value of the maximum queue depth
        /// and the service returned tracking Id associated with this particular error, and marks it as non-transient.
        /// </summary>
        /// <param name="maximumQueueDepth">The maximum number of messages that can be enqueued to the message queue.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public DeviceMaximumQueueDepthExceededException(int maximumQueueDepth, string trackingId)
            : base($"Device Queue depth cannot exceed {maximumQueueDepth} messages", trackingId)
        {
            MaximumQueueDepth = maximumQueueDepth;
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMaximumQueueDepthExceededException"/> with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceMaximumQueueDepthExceededException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceMaximumQueueDepthExceededException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceMaximumQueueDepthExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal DeviceMaximumQueueDepthExceededException()
            : base()
        {
        }

        private DeviceMaximumQueueDepthExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            MaximumQueueDepth = info.GetInt32("MaximumQueueDepth");
        }

        internal int MaximumQueueDepth { get; private set; }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MaximumQueueDepth", MaximumQueueDepth);
        }
    }
}
