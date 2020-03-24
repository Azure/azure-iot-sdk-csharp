// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt to communicate with the IoT Hub service fails.
    /// </summary>
    [Serializable]
    public sealed class IotHubCommunicationException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubCommunicationException"/> class.
        /// </summary>
        public IotHubCommunicationException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubCommunicationException"/> class with the message string set to the message parameter.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        public IotHubCommunicationException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubCommunicationException"/> class with the message string set to the message parameter and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public IotHubCommunicationException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubCommunicationException"/> class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        private IotHubCommunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
