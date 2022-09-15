// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The device/module's response to a direct method invocation.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Gets or sets the status of device method invocation.
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; internal set; }

        /// <summary>
        /// Get the payload object. May be null or empty.
        /// </summary>
        /// <remarks>
        /// The payload can be null or Primitive type (e.g., String, Int)/Array/List/Map/custom type.
        /// </remarks>
        [JsonIgnore]
        public object Payload { get; set; }

        [JsonProperty("payload")]
        internal JRaw JsonPayload { get; set; }
    }
}
