// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Common.WebApi
{
    /// <summary>
    /// An apparently unused class
    /// </summary>
    [Obsolete("This class appears to be unreferenced")]
    public class ResourceRequest
    {
        /// <summary>
        /// The name.
        /// </summary>
        [DataMember]
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The type.
        /// </summary>
        [DataMember]
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// TODO: hard code locations in enum.
        /// </summary>
        [DataMember]
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// TODO: No more than 15 tags, max length per key is 512 chars.
        /// </summary>
        [DataMember]
        [JsonProperty("tags")]
        public IDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Additional properties.
        /// </summary>
        [DataMember]
        [JsonProperty("properties")]
        public IDictionary<string, object> Properties { get; set; }
    }
}
