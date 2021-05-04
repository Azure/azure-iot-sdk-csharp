using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A list of common status codes to represent the response from the client.
    /// </summary>
    /// <remarks>
    /// These status codes are based on the HTTP status codes listed here <see href="http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml"/>
    /// </remarks>
    public static class StatusCodes
    {
        /// <summary>
        /// Status code 200.
        /// </summary>
        public static int Completed => 200;
        /// <summary>
        /// Status code 202.
        /// </summary>
        public static int Pending => 202;
        /// <summary>
        /// Status code 400.
        /// </summary>
        public static int Invalid => 400;
        /// <summary>
        /// Status code 404.
        /// </summary>
        public static int NotFound => 404;
    }
}
