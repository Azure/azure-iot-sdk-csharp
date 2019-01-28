// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the IoT Hub returned an error code.
    /// </summary>
    [Serializable]
    public sealed class ServerErrorException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerErrorException"/> class.
        /// </summary>
        public ServerErrorException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerErrorException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServerErrorException(string message)
            : base(message, isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerErrorException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception.</param>
        public ServerErrorException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }

#if !NETSTANDARD1_3
        ServerErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
