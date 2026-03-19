// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// The class that defines the structure of a desired property update acknowledgement.
    /// </summary>
    internal class PropertyAcknowledgement
    {
        /// <summary>
        /// The unserialized property value.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        [JsonProperty("ac")]
        public int AckCode { get; set; }

        /// <summary>
        /// The acknowledgment version, as supplied in the property update request.
        /// </summary>
        [JsonProperty("av")]
        public long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgment description, an optional, human-readable message about the result of the property update.
        /// </summary>
        [JsonProperty("ad")]
        public string AckDescription { get; set; }
    }
}
