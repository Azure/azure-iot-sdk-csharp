// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Azure IOT Configuration Metrics
    /// </summary>
    public class ConfigurationMetrics
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationMetrics"/>
        /// </summary>
        public ConfigurationMetrics()
        {
            Results = new Dictionary<string, long>();
            Queries = new Dictionary<string, string>();
        }

        /// <summary>
        /// Results of the metrics collection queries
        /// </summary>
        [JsonProperty("results")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Public facing property cannot be modified since it will be a breaking change.")]
        public IDictionary<string, long> Results { get; set; }

        /// <summary>
        /// Queries used for metrics collection
        /// </summary>
        [JsonProperty("queries")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Public facing property cannot be modified since it will be a breaking change.")]
        public IDictionary<string, string> Queries { get; set; }
    }
}
