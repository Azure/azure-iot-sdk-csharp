// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service query specification with a JSON serializer.
    /// </summary>
    public sealed class QuerySpecification
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="query">The query to issue.</param>
        public QuerySpecification(string query)
        {
            Query = query;
        }

        /// <summary>
        /// The query to issue.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; }
    }
}
