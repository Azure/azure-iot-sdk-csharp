// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The device/module's response to a direct method invocation.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        public DirectMethodResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// The status of direct method response.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// The optional direct method payload.
        /// </summary>
        /// <remarks>
        /// The payload can be null or primitive type (e.g., string, int/array/list/dictionary/custom type)
        /// </remarks>
        public object Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the JSON payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP.
        /// </remarks>
        [JsonIgnore]
        internal string RequestId { get; set; }

        /// <summary>
        /// The convention to use with the direct method payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

        internal byte[] GetPayloadObjectBytes()
        {
            return Payload == null
                ? null
                : PayloadConvention.GetObjectBytes(Payload); ;
        }
    }
}
