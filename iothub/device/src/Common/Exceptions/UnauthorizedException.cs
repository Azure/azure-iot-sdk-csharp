// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when there is an authorization error.
    /// </summary>
    /// <remarks>
    /// This exception means the client is not authorized to use the specified IoT hub. Please review the
    /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized">401003 IoTHubUnauthorized</see> 
    /// guide for more information.
    /// </remarks>
    [Serializable]
    public sealed class UnauthorizedException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public UnauthorizedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public UnauthorizedException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception instance that caused the current exception.</param>
        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }
    }
}
