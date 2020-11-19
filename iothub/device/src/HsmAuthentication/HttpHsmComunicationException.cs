// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// The exception that is thrown when communication fails with HSM HTTP server.
    /// </summary>
    public class HttpHsmComunicationException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="HttpHsmComunicationException"/> with the supplied error message and status code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">Status code of the communication failure.</param>
        public HttpHsmComunicationException(string message, int statusCode) : base($"{message}, StatusCode: {statusCode}")
        {
            StatusCode = statusCode;
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
