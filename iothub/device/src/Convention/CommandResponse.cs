// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The command response that the client responds with.
    /// </summary>
    public sealed class CommandResponse
    {
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
        /// <param name="status">A status code indicating success or failure.</param>
        /// <param name="payload">The command response payload that will be serialized using <see cref="DeviceClient.PayloadConvention"/>.</param>
        public CommandResponse(int status, object payload)
        {
            Status = status;
            Payload = payload;
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
        public int Status { get; set; }

        /// <summary>
        /// The command response payload that will be serialized using <see cref="ClientOptions.PayloadConvention"/>.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// The serialized command response data.
        /// </summary>
        internal string GetPayloadAsString()
        {
            return Payload == null
                ? null
                : PayloadConvention.PayloadSerializer.SerializeToString(Payload);
        }

        internal byte[] GetPayloadAsBytes()
        {
            return Payload == null
                ? null
                : PayloadConvention.PayloadEncoder.ContentEncoding.GetBytes(GetPayloadAsString());
        }
    }
}
