// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents the method invocation result.
    /// </summary>
    public class DirectMethodResponse
    {
        // For serialization
        internal DirectMethodResponse()
        {
        }

        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        /// <param name="status">The direct method response's status code.</param>
        /// <param name="payload">The direct method response's optional payload.</param>
        public DirectMethodResponse(int status, byte[] payload = default)
        {
            Status = status;
            Payload = payload;
        }

        /// <summary>
        /// Gets or sets the status of device method invocation.
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonIgnore]
        internal byte[] Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; set; }
    }
}
