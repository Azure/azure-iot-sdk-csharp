// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Device configurations provide the ability to perform IoT device configuration at scale.
    /// You can define configurations and summarize compliance as the configuration is applied.
    /// </summary>
    /// <remarks>
    /// See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/> for more details.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "Cannot change type names as it is considered a breaking change.")]
    public class Configuration : IETagHolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        /// <param name="configurationId">The configuration Id. Lowercase and the following special characters are allowed: [-+%_*!'].</param>
        public Configuration(string configurationId)
            : this()
        {
            Id = configurationId;
        }

        /// <summary>
        /// Initializes a new instance of the Configuration class.
        /// </summary>
        internal Configuration()
        {
            SchemaVersion = "1.0";
            ContentType = "assignment";
        }

        /// <summary>
        /// Gets the identifier for the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; internal set; }

        /// <summary>
        /// Gets Schema version for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string SchemaVersion { get; }

        /// <summary>
        /// Gets or sets labels for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "labels", NullValueHandling = NullValueHandling.Ignore)]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets content for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationContent Content { get; set; } = new ConfigurationContent();

        /// <summary>
        /// Gets the content type for configuration
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; }

        /// <summary>
        /// Gets or sets target condition for the configuration
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
        /// Gets or sets priority for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        /// <summary>
        /// System configuration metrics
        /// </summary>
        [JsonProperty(PropertyName = "systemMetrics", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetrics SystemMetrics { get; internal set; } = new ConfigurationMetrics();

        /// <summary>
        /// Custom configuration metrics
        /// </summary>
        [JsonProperty(PropertyName = "metrics", NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetrics Metrics { get; set; } = new ConfigurationMetrics();

        /// <summary>
        /// Gets or sets configuration's ETag
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string ETag { get; set; }
    }
}
