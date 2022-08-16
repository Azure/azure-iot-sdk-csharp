// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// This exception is thrown when the IoT hub has been suspended. This is likely due to exceeding Azure
    /// spending limits. To resolve the error, check the Azure bill and ensure there are enough credits.
    /// </summary>
    [Serializable]
    public class IotHubSuspendedException : IotHubClientException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IotHubSuspendedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        public IotHubSuspendedException(string iotHubName)
            : base("Iothub {0} is suspended".FormatInvariant(iotHubName), isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        /// <param name="trackingId">Tracking identifier for telemetry purposes.</param>
        public IotHubSuspendedException(string iotHubName, string trackingId)
            : base("Iothub {0} is suspended".FormatInvariant(iotHubName), isTransient: false, trackingId: trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public IotHubSuspendedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Creates an instance of with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The serialized data about the exception being thrown.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected IotHubSuspendedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
