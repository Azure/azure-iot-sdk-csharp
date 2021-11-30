// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Metrics for device/module configurations.
    /// </summary>
    /// <remarks>
    /// See <see cref="Configuration"/> for more details.
    /// </remarks>
    public class ConfigurationMetrics
    {
        /// <summary>
        /// Results of the metrics collection queries
        /// </summary>
        [JsonProperty("results")]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Public facing property cannot be modified since it will be a breaking change.")]
        public IDictionary<string, long> Results { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Queries used for metrics collection
        /// </summary>
        [JsonProperty("queries")]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Public facing property cannot be modified since it will be a breaking change.")]
        public IDictionary<string, string> Queries { get; set; } = new Dictionary<string, string>();
    }
}
