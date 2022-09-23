// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// The optional direct method payload.
        /// </summary>
        /// <remarks>
        /// The payload can be null or primitive type (e.g., string, int/array/list/dictionary/custom type)
        /// </remarks>
        [JsonIgnore]
        public object Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the Json payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP.
        /// </remarks>
        [JsonIgnore]
        internal string RequestId { get; set; }
    }
}
