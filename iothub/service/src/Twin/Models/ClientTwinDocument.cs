// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents twin properties.
    /// </summary>
    public class ClientTwinDocument
    {
        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonProperty("desired", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ClientTwinProperties Desired { get; set; } = new();

        /// <summary>
        /// Gets and sets the twin reported properties.
        /// </summary>
        [JsonProperty("reported", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ClientTwinProperties Reported { get; set; } = new();
    }
}
