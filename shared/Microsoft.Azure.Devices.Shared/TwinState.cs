// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Twin State
    /// </summary>
    public class TwinState
    {
        /// <summary>
        /// Creates an instance of <see cref="TwinState"/>
        /// </summary>
        public TwinState()
        {
            Tags = new TwinCollection();
            DesiredProperties = new TwinCollection();
        }

        /// <summary>
        /// Gets and sets the <see cref="Twin"/> tags.
        /// </summary>
        [JsonProperty(PropertyName = "tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }
        
        /// <summary>
        /// Gets and sets the <see cref="Twin"/> desired properties.
        /// </summary>
        [JsonProperty(PropertyName = "desiredProperties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection DesiredProperties { get; set; }
    }
}
