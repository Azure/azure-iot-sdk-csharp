// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Devices.Client.Extensions;

    /// <summary>
    /// The exception that is thrown when the IoT Hub has been suspended.
    /// </summary>
    [Serializable]
    public class IotHubSuspendedException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubSuspendedException"/> class.
        /// </summary>
        public IotHubSuspendedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubSuspendedException"/> class.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        public IotHubSuspendedException(string iotHubName)
            : base("Iothub {0} is suspended".FormatInvariant(iotHubName), isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubSuspendedException"/> class.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        /// <param name="trackingId">Tracking identifier for telemetry purposes.</param>
        public IotHubSuspendedException(string iotHubName, string trackingId)
            : base("Iothub {0} is suspended".FormatInvariant(iotHubName), isTransient: false, trackingId: trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMaximumQueueDepthExceededException"/> class with the message string set to the message parameter and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public IotHubSuspendedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

#if !NETSTANDARD1_3
        IotHubSuspendedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
