// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service query specification with a JSON serializer.
    /// </summary>
    internal class QuerySpecification
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="query"></param>
        internal QuerySpecification(string query)
        {
            Query = query;
        }

        /// <summary>
        /// Operation mode
        /// </summary>
        [JsonProperty("query")]
        internal string Query { get; set; }
    }
}
