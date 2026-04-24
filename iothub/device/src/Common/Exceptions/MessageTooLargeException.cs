// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt to send a message fails because the length of the message
    /// exceeds the maximum size allowed.
    /// </summary>
    /// <remarks>
    /// When the message is too large for IoT hub you will receive this exception. You should attempt to reduce
    /// your message size and send again. For more information on message sizes, see
    /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits">IoT hub quotas and throttling | Other limits</see>
    /// </remarks>
    [Serializable]
    public sealed class MessageTooLargeException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public MessageTooLargeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the maximum sized
        /// allowed for a message, in bytes.
        /// </summary>
        /// <param name="maximumMessageSizeInBytes">Device identifier that already exists.</param>
        public MessageTooLargeException(int maximumMessageSizeInBytes)
            : base("Message size cannot exceed {0} bytes".FormatInvariant(maximumMessageSizeInBytes), isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string
        /// has been localized for the current system culture.</param>
        public MessageTooLargeException(string message)
            : base(message, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string has been
        /// localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public MessageTooLargeException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }
    }
}
