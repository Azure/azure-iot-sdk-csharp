// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The command response that the client responds with.
    /// </summary>
    public sealed class CommandResponse
    {
        private readonly object _result;

        internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Creates a new instance of the class with the associated command response data and a status code.
        /// </summary>
        /// <param name="result">The command response data.</param>
        /// <param name="status">A status code indicating success or failure.</param>
        public CommandResponse(object result, int status)
        {
            _result = result;
            Status = status;
        }

        /// <summary>
        /// Creates a new instance of the class with the associated status code.
        /// </summary>
        /// <param name="status">A status code indicating success or failure.</param>
        public CommandResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// The command response status code indicating success or failure.
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        /// The serialized command response data.
        /// </summary>
        public string ResultAsJson => _result == null ? null : PayloadConvention.PayloadSerializer.SerializeToString(_result);

        internal byte[] ResultAsBytes => _result == null ? null : Encoding.UTF8.GetBytes(ResultAsJson);
    }
}
