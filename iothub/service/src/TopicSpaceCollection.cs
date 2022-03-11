// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    ///  A collection of topic spaces.
    /// </summary>
    public class TopicSpaceCollection
    {
        /// <summary>
        /// The topic spaces.
        /// </summary>
        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public List<TopicSpace> Value { get; }

        /// <summary>
        /// A URI to retrieve the next page of results.
        /// </summary>
        [JsonProperty(PropertyName = "nextLink", NullValueHandling = NullValueHandling.Ignore)]
        public string NextLink { get; }
    }
}