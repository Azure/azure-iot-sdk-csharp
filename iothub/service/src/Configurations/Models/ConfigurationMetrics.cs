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
        [JsonProperty("results")]
        public IDictionary<string, long> Results { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Queries used for metrics collection.
        /// </summary>
        [JsonProperty("queries")]
        public IDictionary<string, string> Queries { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <remarks>
        /// To give the properties above a default instance to prevent <see cref="NullReferenceException"/> but
        /// avoid serializing them when the dictionary is empty, we use this feature of Newtonsoft.Json, which must
        /// be public, and hide it from web docs and intellisense using the EditorBrowsable attribute.
        /// </remarks>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeResults()
        {
            return Results != null && Results.Any();
        }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <remarks>
        /// To give the properties above a default instance to prevent <see cref="NullReferenceException"/> but
        /// avoid serializing them when the dictionary is empty, we use this feature of Newtonsoft.Json, which must
        /// be public, and hide it from web docs and intellisense using the EditorBrowsable attribute.
        /// </remarks>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeQueries()
        {
            return Queries != null && Queries.Any();
        }
    }
}
