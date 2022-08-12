// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when there is an authorization error.
    /// </summary>
    /// <remarks>
    ///  This exception means the client is not authorized to use the specified IoT hub.
    ///  Please review the <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized">401003 IoTHubUnauthorized</see>
    ///  guide for more information.
    /// </remarks>
    [Serializable]
    public sealed class UnauthorizedException : IotHubException
    {
        /// <summary>
        /// Creates an instance of this class with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message"></param>
        public UnauthorizedException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified <see cref="ErrorCode"/>, error message
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public UnauthorizedException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

        internal UnauthorizedException()
            : base()
        {
        }
    }
}
