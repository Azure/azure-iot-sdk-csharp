// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the IoT hub returned an internal service error.
    /// </summary>
    /// <remarks>
    /// This exception typically means the IoT hub service has encountered an unexpected error and is usually transient.
    /// Please review the
    /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-500xxx-internal-errors">500xxx Internal errors</see>
    /// guide for more information. The best course of action is to retry your operation after some time. By default,
    /// the SDK will utilize the <see cref="ExponentialBackoff"/> retry strategy.
    /// </remarks>
    [Serializable]
    public sealed class ServerErrorException : IotHubClientException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ServerErrorException()
            : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServerErrorException(string message)
            : base(message, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception instance that caused the current exception.</param>
        public ServerErrorException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }
    }
}
