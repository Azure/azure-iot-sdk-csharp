// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A list of common status codes to represent the response from the client.
    /// </summary>
    /// <remarks>
    /// These status codes are based on the HTTP status codes listed here <see href="http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml"/>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "To allow customers to extend this class we need to not mark it static.")]
    public class StatusCodes
    {
        /// <summary>
        /// Status code 200.
        /// </summary>
        public static int OK => 200;
        /// <summary>
        /// Status code 202.
        /// </summary>
        public static int Accepted => 202;
        /// <summary>
        /// Status code 400.
        /// </summary>
        public static int BadRequest => 400;
        /// <summary>
        /// Status code 404.
        /// </summary>
        public static int NotFound => 404;
    }
}
