// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class QuerySpecification
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("query")]
        public string Sql { get; set; }
    }
}
