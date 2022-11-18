// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains the result of a twin, scheduled job or raw query.
    /// </summary>
    internal class QueriedPage<T>
    {
        private const string ContinuationTokenHeader = "x-ms-continuation";

        // Payload is taken separately from http response because reading the payload should only be done
        // in an async function.
        internal QueriedPage(HttpResponseMessage response, string payload)
        {
            Items = JsonSerializer.Deserialize<IEnumerable<T>>(payload);
            ContinuationToken = response.Headers.GetFirstValueOrNull(ContinuationTokenHeader);
        }

        [JsonPropertyName("items")]
        internal IEnumerable<T> Items { get; set; }

        [JsonPropertyName("continuationToken")]
        internal string ContinuationToken { get; set; }
    }
}
