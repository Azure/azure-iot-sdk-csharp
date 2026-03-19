// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Azure;
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
    public class Configuration
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        /// <param name="configurationId">
        /// The configuration Id.
        /// Lowercase and the following special characters are allowed: [-+%_*!'].
        /// </param>
        public Configuration(string configurationId)
        {
            Id = configurationId;
        }

        /// <summary>
        /// The unique identifier of the configuration.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <summary>
        /// The schema version of the configuration.
        /// </summary>
        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; } = "1.0";

        /// <summary>
        /// The key-value pairs used to describe the configuration.
        /// </summary>
        [JsonProperty("labels")]
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The content of the configuration.
        /// </summary>
        [JsonProperty("content")]
        public ConfigurationContent Content { get; set; } = new();

        /// <summary>
        /// Gets the content type for configuration.
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; } = "assignment";

        /// <summary>
        /// The query used to define the targeted devices or modules.
        /// </summary>
        /// <remarks>
        /// The query is based on twin tags and/or reported properties.
        /// </remarks>
        [JsonProperty("targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// The creation date and time of the configuration.
        /// </summary>
        [JsonProperty("createdTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? CreatedOnUtc { get; internal set; }

        /// <summary>
        /// The update date and time of the configuration.
        /// </summary>
        [JsonProperty("lastUpdatedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// The priority number assigned to the configuration.
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// The system metrics computed by the IoT hub that cannot be customized.
        /// </summary>
        [JsonProperty("systemMetrics")]
        public ConfigurationMetrics SystemMetrics { get; set; } = new();

        /// <summary>
        /// The custom metrics specified by the developer as queries against twin reported properties.
        /// </summary>
        [JsonProperty("metrics")]
        public ConfigurationMetrics Metrics { get; set; } = new();

        /// <summary>
        /// The ETag of the configuration.
        /// </summary>
        [JsonProperty("etag")]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly.
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }
    }
}
