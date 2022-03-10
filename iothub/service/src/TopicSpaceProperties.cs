// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The data structure represent the TopicSpaceProperties
    /// </summary>
    public class TopicSpaceProperties
    {
        /// <summary>
        /// deletedTimeUtc
        /// </summary>
        [JsonProperty(PropertyName = "deletedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DeletedTimeUtc { get; }
        /// <summary>
        /// deleted
        /// </summary>
        [JsonProperty(PropertyName = "deleted", NullValueHandling = NullValueHandling.Ignore)]
        public bool Deleted { get; }
        /// <summary>
        /// topicTemplates
        /// </summary>
        [JsonProperty(PropertyName = "topicTemplates", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TopicTemplates { get; set; }
        /// <summary>
        /// purgeTimeUtc
        /// </summary>
        [JsonProperty(PropertyName = "purgeTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PurgeTimeUtc { get; }
        /// <summary>
        /// topicSpaceType
        /// </summary>
        [JsonProperty(PropertyName = "topicSpaceType", NullValueHandling = NullValueHandling.Ignore)]
        public TopicSpaceTypeEnum TopicSpaceType { get; set; }

        /// <summary>
        /// The data structure represent the TopicSpaceTypeEnum
        /// </summary>
        public enum TopicSpaceTypeEnum
        {
            /// <summary>
            /// LowFanout
            /// </summary>
            LowFanout = 1,
            /// <summary>
            /// HighFanout
            /// </summary>
            HighFanout = 2,
            /// <summary>
            /// PublishOnly
            /// </summary>
            PublishOnly = 3
        }
    }
}