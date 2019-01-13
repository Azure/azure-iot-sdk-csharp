// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Devices.Client.Extensions;

    // TODO: #707 - This exception is not thrown by any protocol.

    /// <summary>
    /// The exception that is thrown when the IoT hub instance is not found.
    /// </summary>
    [Serializable]
    public class IotHubNotFoundException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubNotFoundException"/> class.
        /// </summary>
        public IotHubNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubNotFoundException"/> class with the message string containing the name of the IoT hub instance that couldn't be found.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        public IotHubNotFoundException(string iotHubName)
            : base("IoT hub not found: {0}".FormatInvariant(iotHubName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubNotFoundException"/> class with the message string containing the name of the IoT hub instance that couldn't be found.
        /// </summary>
        /// <param name="iotHubName">IoT hub name that could not be found.</param>
        /// <param name="trackingId">Tracking identifier for telemetry purposes.</param>
        public IotHubNotFoundException(string iotHubName, string trackingId)
            : base("IoT hub not found: {0}".FormatInvariant(iotHubName), trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception.</param>
        public IotHubNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETSTANDARD1_3
        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubNotFoundException"/> class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        IotHubNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
