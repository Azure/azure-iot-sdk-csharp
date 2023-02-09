// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// The exception that is thrown when communication fails with HSM HTTP server.
    /// </summary>
    [Serializable]
    internal sealed class HttpHsmComunicationException : Exception
    {
        /// <summary>
        /// Creates an instance of this class with the supplied error message and status code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">Status code of the communication failure.</param>
        /// <param name="innerException">The inner exception, if any.</param>
        public HttpHsmComunicationException(string message, int statusCode, Exception innerException = default)
            : base($"{message}, StatusCode: {statusCode}", innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates an instance of this class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        internal HttpHsmComunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Status code of the communication failure.
        /// </summary>
        public int StatusCode { get; }
    }
}
