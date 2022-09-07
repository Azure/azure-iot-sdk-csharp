// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the IoT hub is busy.
    /// </summary>
    /// <remarks>
    /// This exception typically means the service is unavailable due to high load or an unexpected error and is usually transient.
    /// The best course of action is to retry your operation after some time.
    /// </remarks>
    [Serializable]
    public sealed class ServerBusyException : IotHubServiceException
    {
        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified error message and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified <see cref="IotHubStatusCode"/>, error message
        /// and marks it as transient.
        /// </summary>
        /// <param name="code">The <see cref="IotHubStatusCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public ServerBusyException(IotHubStatusCode code, string message)
            : base(code, message, true)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ServerBusyException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

        internal ServerBusyException()
            : base()
        {
        }
    }
}
