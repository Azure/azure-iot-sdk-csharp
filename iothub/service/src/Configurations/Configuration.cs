// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The configuration for IoT hub device and module twins.
    /// </summary>
    /// <remarks>
    /// Device configurations provide the ability to perform IoT device configuration at scale.
    /// You can define configurations and summarize compliance as the configuration is applied.
    /// See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/> for more details.
    /// </remarks>
    public class Configuration : IETagHolder
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        /// <param name="configurationId">
        /// The configuration Id.
        /// Lowercase and the following special characters are allowed: [-+%_*!'].
        /// </param>
        public Configuration(string configurationId)
            : this()
        {
            Id = configurationId;
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        internal Configuration()
        {
            SchemaVersion = "1.0";
            ContentType = "assignment";
        }

        /// <summary>
        /// The unique identifier of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; internal set; }

        /// <summary>
        /// The schema version of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string SchemaVersion { get; }

        /// <summary>
        /// The key-value pairs used to describe the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "labels", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The content of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationContent Content { get; set; }

        /// <summary>
        /// Gets the content type for configuration.
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; internal set; }

        /// <summary>
        /// The query used to define the targeted devices or modules.
        /// </summary>
        /// <remarks>
        /// The query is based on twin tags and/or reported properties.
        /// </remarks>
        [JsonProperty(PropertyName = "targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// The creation date and time of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "createdTimeUtc")]
        public DateTime CreatedTimeUtc { get; internal set; }

        /// <summary>
        /// The update date and time of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedTimeUtc")]
        public DateTime LastUpdatedTimeUtc { get; internal set; }

        /// <summary>
        /// The priority number assigned to the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        /// <summary>
        /// The system metrics computed by the IoT hub that cannot be customized.
        /// </summary>
        [JsonProperty(PropertyName = "systemMetrics", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetrics SystemMetrics { get; set; }

        /// <summary>
        /// The custom metrics specified by the developer as queries against twin reported properties.
        /// </summary>
        [JsonProperty(PropertyName = "metrics", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetrics Metrics { get; set; }

        /// <summary>
        /// The ETag of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeLabels()
        {
            return Labels != null & Labels.Any();
        }
    }
}
