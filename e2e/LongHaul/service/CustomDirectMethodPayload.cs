// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Use a custom payload for direct method invocation to test object serialization using several data types.
    /// </summary>
    internal class CustomDirectMethodPayload
    {
        [JsonProperty("randomId")]
        public Guid RandomId { get; set; }

        [JsonProperty("currentTimeUtc")]
        public DateTimeOffset CurrentTimeUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonProperty("methodCallsCount")]
        public long MethodCallsCount { get; set; }
    }
}
