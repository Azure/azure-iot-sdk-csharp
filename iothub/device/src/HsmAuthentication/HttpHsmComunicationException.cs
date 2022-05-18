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
    public class HttpHsmComunicationException : Exception
    {
        /// <summary>
        /// Creates an instance of this class with the supplied error message and status code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">Status code of the communication failure.</param>
        public HttpHsmComunicationException(string message, int statusCode) : base($"{message}, StatusCode: {statusCode}")
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the this class class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        protected HttpHsmComunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal HttpHsmComunicationException()
            : base()
        {
        }

        internal HttpHsmComunicationException(string message)
            : base(message)
        {
        }

        internal HttpHsmComunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Status code of the communication failure.
        /// </summary>
        public int StatusCode { get; }
    }
}
