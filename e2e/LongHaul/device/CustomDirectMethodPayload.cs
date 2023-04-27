﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    /// <summary>
    /// Use a custom payload for direct method invocation to test object serialization using several data types.
    /// </summary>
    internal class CustomDirectMethodPayload
    {
        [JsonProperty("randomId")]
        [JsonPropertyName("randomId")]
        public Guid RandomId { get; set; }

        [JsonProperty("sentTimeUtc")]
        [JsonPropertyName("sentTimeUtc")]
        public DateTimeOffset SentOnUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonProperty("methodCallsCount")]
        [JsonPropertyName("methodCallsCount")]
        public long MethodCallsCount { get; set; }
    }
}