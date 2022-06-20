﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service query specification with a JSON serializer.
    /// </summary>
    public class QuerySpecification
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="query"></param>
        public QuerySpecification(string query)
        {
            Query = query;
        }

        /// <summary>
        /// Operation mode
        /// </summary>
        [JsonProperty(PropertyName = "query", Required = Required.Always)]
        public string Query { get; set; }
    }
}
