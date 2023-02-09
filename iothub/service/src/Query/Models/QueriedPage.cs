// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains the result of a twin, scheduled job or raw query.
    /// </summary>
    internal sealed class QueriedPage<T>
    {
        private const string ContinuationTokenHeader = "x-ms-continuation";

        // Payload is taken separately from http response because reading the payload should only be done
        // in an async function.
        internal QueriedPage(HttpResponseMessage response, string payload)
        {
            Items = JsonConvert.DeserializeObject<IEnumerable<T>>(payload);
            ContinuationToken = response.Headers.GetFirstValueOrNull(ContinuationTokenHeader);
        }

        [JsonProperty("items")]
        internal IEnumerable<T> Items { get; set; }

        [JsonProperty("continuationToken")]
        internal string ContinuationToken { get; set; }
    }
}
