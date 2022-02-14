// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The command response that the client responds with.
    /// </summary>
    public sealed class CommandResponse
    {
        private readonly object _payload;

        internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Public constructor provided only for mocking purposes.
        /// </summary>
        public CommandResponse()
        {
        }

        /// <summary>
        /// Creates a new instance of the class with the associated command response data and a status code.
        /// </summary>
        /// <param name="payload">The command response payload.</param>
        /// <param name="status">A status code indicating success or failure.</param>
        public CommandResponse(object payload, int status)
        {
            _payload = payload;
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
        public int Status { get; }

        /// <summary>
        /// The command response payload.
        /// </summary>
        public object Payload { get; }

        /// <summary>
        /// The serialized command response data.
        /// </summary>
        internal string GetPayloadAsString()
        {
            return _payload == null
                ? null
                : PayloadConvention.PayloadSerializer.SerializeToString(_payload);
        }

        internal byte[] GetPayloadAsBytes()
        {
            return _payload == null
                ? null
                : PayloadConvention.PayloadEncoder.ContentEncoding.GetBytes(GetPayloadAsString());
        }
    }
}
