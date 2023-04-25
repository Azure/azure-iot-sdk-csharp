// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests
{
    internal class StjCustomPayload
    {
        [JsonPropertyName("string")]
        public string StringProperty { get; set; }

        [JsonPropertyName("guid")]
        public string GuidProperty { get; set; }
    }
}
