// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Common.Extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The query result.
    /// </summary>
    public class DpsQueryResult
    {
        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string QueryResultTypeHeader = "x-ms-item-type";
        /// <summary>
        /// The query result type.
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public DpsQueryResultType Type { get; set; }

        /// <summary>
        /// The query result items, as a collection.
        /// </summary>
        [JsonProperty(PropertyName = "items", Required = Required.Always)]
        public IEnumerable<object> Items { get; set; }

        /// <summary>
        /// Request continuation token.
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken", Required = Required.AllowNull)]
        public string ContinuationToken { get; set; }

        internal static async Task<DpsQueryResult> FromHttpResponseAsync(HttpResponseMessage response)
        {
            return new DpsQueryResult
            {
#if WINDOWS_UWP || NETSTANDARD1_3 || NETSTANDARD2_0
                Items = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<object>>(await response.Content.ReadAsStringAsync()),
#else
                Items = await response.Content.ReadAsAsync<IEnumerable<object>>(),
#endif
                Type = (DpsQueryResultType)Enum.Parse(typeof(DpsQueryResultType), response.Headers.GetFirstValueOrNull(QueryResultTypeHeader) ?? "unknown"),
                ContinuationToken = response.Headers.GetFirstValueOrNull(ContinuationTokenHeader)
            };
        }
    }
}