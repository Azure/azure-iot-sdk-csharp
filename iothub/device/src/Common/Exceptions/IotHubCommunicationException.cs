// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// This exception is thrown when an attempt to communicate with the IoT hub service fails due to transient
    /// network errors after exhausting all the retries based on the retry policy set on the client or
    /// due to operation timeouts.
    /// </summary>
    /// <remarks>
    /// By default, the SDK indefinitely retries dropped connections, unless the retry policy is overridden.
    /// For more information on the SDK's retry policy and how to override it, see <see href="https://github.com/Azure/azure-iot-sdk-csharp/blob/main/iothub/device/devdoc/retrypolicy.md"/>.
    /// When the exception is thrown due to operation timeouts, the inner exception will have OperationCanceledException.
    /// Retrying operations failed due to timeouts could resolve the error.
    /// </remarks>
    [Serializable]
    public sealed class IotHubCommunicationException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IotHubCommunicationException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        public IotHubCommunicationException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public IotHubCommunicationException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        private IotHubCommunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
