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
        /// Creates a new instance of the class with the associated status code and response payload.
        /// </summary>
        /// <param name="status">A status code indicating success or failure (e.g., 200, 400); see <see cref="CommonClientResponseCodes"/>.</param>
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
        /// <remarks>
        /// This is usually an HTTP Status Code e.g. 200, 400.
        /// Some commonly used codes are defined in <see cref="CommonClientResponseCodes" />.
        /// </remarks>
        public int Status { get; set; }

        /// <summary>
        /// The command response payload that will be serialized using <see cref="ClientOptions.PayloadConvention"/>.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// The serialized command response payload.
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
