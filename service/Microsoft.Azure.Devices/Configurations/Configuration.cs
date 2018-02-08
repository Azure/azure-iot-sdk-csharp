// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.Devices.Shared;

    using Newtonsoft.Json;

    /// <summary>
    /// Azure IoT Edge Configurations.
    /// </summary>
    public class Configuration : IETagHolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class. 
        /// </summary>
        /// <param name="configurationId">
        /// configuration Id
        /// </param>
        public Configuration(string configurationId)
            : this()
        {
            this.Id = configurationId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        internal Configuration()
        {
            this.SchemaVersion = "1.0";
            this.ContentType = "assignment";
        }

        /// <summary>
        /// Gets Identifier for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; internal set; }

        /// <summary>
        /// Gets Schema version for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion", Required = Required.Always)]
        public string SchemaVersion { get; internal set; }

        /// <summary>
        /// Gets or sets labels for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "labels", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Labels { get; set; }

        /// <summary>
        /// Gets or sets Content for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationContent Content { get; set; }

        /// <summary>
        /// Gets the content type for configuration
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; internal set; }

        /// <summary>
        /// Gets or sets Target Condition for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// Gets creation time for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "createdTimeUtc")]
        public DateTime CreatedTimeUtc { get; internal set; }

        /// <summary>
        /// Gets last update time for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedTimeUtc")]
        public DateTime LastUpdatedTimeUtc { get; internal set; }

        /// <summary>
        /// Gets or sets Priority for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Gets the configuration statistics in form of metric name and metric value pairs.
        /// </summary>
        [JsonProperty(PropertyName = "statistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, long> Statistics { get; internal set; }

        /// <summary>
        /// Gets or sets configuration's ETag
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string ETag { get; set; }
    }
}
#endif