// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

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
            this.Results = new Dictionary<string, long>();
            this.Queries = new Dictionary<string, string>();
        }

        /// <summary>
        /// Results of the metrics collection queries
        /// </summary>
        [JsonProperty("results")]
        public IDictionary<string, long> Results { get; set; }

        /// <summary>
        /// Queries used for metrics collection
        /// </summary>
        [JsonProperty("queries")]
        public IDictionary<string, string> Queries { get; set; }
    }
}
#endif