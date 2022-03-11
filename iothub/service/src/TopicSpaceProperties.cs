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
        /// The time of deletion when isDeleted is true.
        /// </summary>
        [JsonProperty(PropertyName = "deletedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DeletedTimeUtc { get; }

        /// <summary>
        /// The flag to mark deleted records.
        /// </summary>
        [JsonProperty(PropertyName = "deleted", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsDeleted { get; }

        /// <summary>
        /// The topic templates for the topic space.\r\nFor example, contoso/{principal.deviceid}/telemetry/#"
        /// </summary>
        [JsonProperty(PropertyName = "topicTemplates", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TopicTemplates { get; set; }

        /// <summary>
        /// The time the record will be automatically purged when isDeleted is true.
        /// </summary>
        [JsonProperty(PropertyName = "purgeTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PurgeTimeUtc { get; }

        /// <summary>
        /// The topic space type.\r\nSupported values are `LowFanout`, `HighFanout` and `PublishOnly`
        /// </summary>
        [JsonProperty(PropertyName = "topicSpaceType", NullValueHandling = NullValueHandling.Ignore)]
        public TopicSpaceTypeEnum TopicSpaceType { get; set; }
    }
}