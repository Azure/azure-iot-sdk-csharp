// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
        /// Status code of the communication failure.
        /// </summary>
        public int StatusCode { get; }
    }
}
