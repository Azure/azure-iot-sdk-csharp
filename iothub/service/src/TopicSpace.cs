// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    /// <summary>
    /// The data structure represent the TopicSpace (A topic space defines a set of topics, usually with templated topic expressions,
    /// for a particular use (low fan-out, high fan-out, or publish only).
    /// </summary>
    public class TopicSpace : IETagHolder
    {
        /// <summary>
        /// name
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// properties
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public TopicSpaceProperties Properties { get; set; }

        /// <summary>
        /// name
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }
    }
}