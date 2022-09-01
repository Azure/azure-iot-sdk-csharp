// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents the method invocation result.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        public DirectMethodResponse()
        {
        }

        /// <summary>
        /// The status of direct method response.
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// The optional direct method payload.
        /// </summary>
        [JsonIgnore]
        public byte[] Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; set; }
    }
}
