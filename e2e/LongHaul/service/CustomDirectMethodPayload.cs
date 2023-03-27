// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Use a custom payload for direct method invocation to test object serialization using several data types.
    /// </summary>
    internal class CustomDirectMethodPayload
    {
        [JsonPropertyName("randomId")]
        public Guid RandomId { get; set; }

        [JsonPropertyName("currentTimeUtc")]
        public DateTimeOffset CurrentTimeUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("methodCallsCount")]
        public int MethodCallsCount { get; set; }
    }
}
