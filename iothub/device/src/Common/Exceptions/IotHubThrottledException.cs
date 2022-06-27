// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// This exception is thrown when the requests to the IoT hub exceed the limits based on the tier of the hub.
    /// Retrying with exponential back-off could resolve this error.
    /// </summary>
    /// <remarks>
    /// For information on the IoT hub quotas and throttling, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling"/>.
    /// </remarks>
    [Serializable]
    public sealed class IotHubThrottledException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IotHubThrottledException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter and
        /// a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        public IotHubThrottledException(string message)
            : base(message, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the maximum count of active requests.
        /// </summary>
        /// <param name="maximumBatchCount">Maximum count of active requests.</param>
        public IotHubThrottledException(int maximumBatchCount)
            : base("Device Container has exceeded maximum number of allowed active requests: {0}".FormatInvariant(maximumBatchCount), isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string
        /// has been localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public IotHubThrottledException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        private IotHubThrottledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
