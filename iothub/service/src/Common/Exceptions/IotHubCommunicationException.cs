// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt to communicate with the IoT Hub fails.
    /// </summary>
    [Serializable]
    public sealed class IotHubCommunicationException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="IotHubCommunicationException"/> with a specified error message and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IotHubCommunicationException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubCommunicationException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubCommunicationException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        internal IotHubCommunicationException()
            : base()
        {
        }

        private IotHubCommunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
