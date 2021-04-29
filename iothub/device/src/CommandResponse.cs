// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public sealed class CommandResponse
    {
        private readonly object _result;
        internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Make a new instance of the return class and validates that the payload is correct JSON.
        /// </summary>
        /// <param name="result">data returned by the method call.</param>
        /// <param name="status">status indicating success or failure.</param>
        /// <returns></returns>
        public CommandResponse(object result, int status)
        {
            _result = result;
            Status = status;
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body
        /// </summary>
        /// <param name="status">an integer code containing a method call status.</param>
        public CommandResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// contains the response of the device client application method handler.
        /// </summary>
        public int Status
        {
            get; private set;
        }

        /// <summary>
        /// Property containing the entire result data, in Json format.
        /// </summary>
        public string ResultAsJson => _result == null ? null : PayloadConvention.PayloadSerializer.SerializeToString(_result);

        internal byte[] ResultAsBytes => _result == null ? null : Encoding.UTF8.GetBytes(ResultAsJson);
    }
}
