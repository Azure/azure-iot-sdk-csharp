// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A list of common status codes to represent the response from the client.
    /// </summary>
    /// <remarks>
    /// These status codes are based on the HTTP status codes listed here <see href="http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml"/>.
    /// </remarks>
    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable",
        Justification = "To allow customers to extend this class we need to not mark it static.")]
    public class CommonClientResponseCodes
    {
        /// <summary>
        /// As per HTTP semantics this code indicates that the request has succeeded.
        /// </summary>
        public const int OK = 200;

        /// <summary>
        /// As per HTTP semantics this code indicates that the request has been
        /// accepted for processing, but the processing has not been completed.
        /// </summary>
        public const int Accepted = 202;

        /// <summary>
        /// As per HTTP semantics this code indicates that the server cannot or
        /// will not process the request due to something that is perceived to be a client error
        /// (e.g., malformed request syntax, invalid request message framing, or deceptive request routing).
        /// </summary>
        public const int BadRequest = 400;

        /// <summary>
        /// As per HTTP semantics this code indicates that the origin server did
        /// not find a current representation for the target resource or is not
        /// willing to disclose that one exists.
        /// </summary>
        public const int NotFound = 404;
    }
}
