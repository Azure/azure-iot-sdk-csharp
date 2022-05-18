// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// This exception actually corresponds to IoTHubQuotaExceeded. For more information on what causes this error
    /// and steps to resolve, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded"/>.
    /// The exception type has not been changed to avoid breaking changes but the inner exception has the correct exception type.
    /// </summary>
    [Serializable]
    public sealed class DeviceMaximumQueueDepthExceededException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class with the message string containing the identifier of the already existing device.
        /// </summary>
        public DeviceMaximumQueueDepthExceededException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the identifier of the already existing device.
        /// </summary>
        /// <param name="maximumQueueDepth">Maximum number of messages in the queue.</param>
        public DeviceMaximumQueueDepthExceededException(int maximumQueueDepth)
            : base("Device Queue depth cannot exceed {0} messages".FormatInvariant(maximumQueueDepth), isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        public DeviceMaximumQueueDepthExceededException(string message)
            : base(message, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public DeviceMaximumQueueDepthExceededException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        private DeviceMaximumQueueDepthExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
