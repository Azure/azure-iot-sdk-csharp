// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using System;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class CustomPayload
    {
        [JsonPropertyName("randomId")]
        public Guid RandomId { get; set; }

        [JsonPropertyName("currentTime")]
        public DateTimeOffset CurrentTime { get; set; }

        [JsonPropertyName("methodCallsCount")]
        public int MethodCallsCount { get; set; }
    }
}
