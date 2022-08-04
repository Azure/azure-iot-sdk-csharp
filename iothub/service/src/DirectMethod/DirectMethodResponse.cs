// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The device/module's response to the direct method invocation.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Gets or sets the status of device method invocation.
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; internal set; }

        [JsonProperty("payload")]
        internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// The Json payload sent by the device/module. May be null or empty.
        /// </summary>
        [JsonIgnore]
        public string Payload
        {
            get
            {
                return (string)JsonPayload.Value;
            }
        }
    }
}
