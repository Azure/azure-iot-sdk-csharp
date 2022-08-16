// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the IoT hub is busy.
    /// </summary>
    /// <remarks>
    /// This exception typically means the service is unavailable due to high load or an unexpected error and is usually transient.
    /// The best course of action is to retry your operation after some time.
    /// By default, the SDK will utilize the <see cref="ExponentialBackoff"/> retry strategy.
    /// </remarks>
    [Serializable]
    public sealed class ServerBusyException : IotHubClientException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ServerBusyException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception.</param>
        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }
    }
}
