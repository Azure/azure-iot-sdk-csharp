// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

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
        /// Results of the metrics collection queries.
        /// </summary>
        [JsonPropertyName("results")]
        public IDictionary<string, long> Results { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Queries used for metrics collection.
        /// </summary>
        [JsonPropertyName("queries")]
        public IDictionary<string, string> Queries { get; set; } = new Dictionary<string, string>();
    }
}
