﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using Azure;

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
        [JsonPropertyName("id", Required = Required.Always)]
        public string Id { get; internal set; }

        /// <summary>
        /// The schema version of the configuration.
        /// </summary>
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; } = "1.0";

        /// <summary>
        /// The key-value pairs used to describe the configuration.
        /// </summary>
        [JsonPropertyName("labels")]
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The content of the configuration.
        /// </summary>
        [JsonPropertyName("content")]
        public ConfigurationContent Content { get; set; }

        /// <summary>
        /// Gets the content type for configuration.
        /// </summary>
        [JsonPropertyName("contentType")]
        public string ContentType { get; } = "assignment";

        /// <summary>
        /// The query used to define the targeted devices or modules.
        /// </summary>
        /// <remarks>
        /// The query is based on twin tags and/or reported properties.
        /// </remarks>
        [JsonPropertyName("targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// The creation date and time of the configuration.
        /// </summary>
        [JsonPropertyName("createdTimeUtc")]
        public DateTimeOffset CreatedOnUtc { get; internal set; }

        /// <summary>
        /// The update date and time of the configuration.
        /// </summary>
        [JsonPropertyName("lastUpdatedTimeUtc")]
        public DateTimeOffset LastUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// The priority number assigned to the configuration.
        /// </summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// The system metrics computed by the IoT hub that cannot be customized.
        /// </summary>
        [JsonPropertyName("systemMetrics")]
        public ConfigurationMetrics SystemMetrics { get; set; }

        /// <summary>
        /// The custom metrics specified by the developer as queries against twin reported properties.
        /// </summary>
        [JsonPropertyName("metrics")]
        public ConfigurationMetrics Metrics { get; set; }

        /// <summary>
        /// The ETag of the configuration.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }
    }
}
