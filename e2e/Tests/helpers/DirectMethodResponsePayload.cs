// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class DirectMethodResponsePayload
    {
        [JsonPropertyName("currentState")]
        public string CurrentState { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset TimeStamp { get; set; }

        [JsonPropertyName("nestedObject")]
        public NestedObject NestedObject { get; set; }
    }
}
