// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// This exception is thrown when the IoT hub has been suspended. This is likely due to exceeding Azure
    /// spending limits. To resolve the error, check the Azure bill and ensure there are enough credits.
    /// </summary>
    [Serializable]
    public class IotHubSuspendedException : IotHubException
    {
        private const string SuspendedException = "IotHubSuspendedException";
        private const string IotHubSuspended = "IoT hub {0} is suspended.";

        /// <summary>
        /// Creates an instance of this class with a name of the suspended IoT hub
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub that has been suspended.</param>
        public IotHubSuspendedException(string iotHubName)
            : base(string.Format(CultureInfo.InvariantCulture, IotHubSuspended, iotHubName))
        {
        }

        /// <summary>
        /// Creates an instance of this class with a name of the suspended IoT hub
        /// and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub that has been suspended.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubSuspendedException(string iotHubName, string trackingId)
            : base(string.Format(CultureInfo.InvariantCulture, IotHubSuspended, iotHubName), trackingId: trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [SuppressMessage("Usage", "CA2229:Implement serialization constructors",
            Justification = "Cannot modify public API surface since it will be a breaking change")]
        public IotHubSuspendedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal IotHubSuspendedException()
            : base()
        {
        }

        internal IotHubSuspendedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
