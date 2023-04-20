// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    internal class DeviceTwinCustomProperty
    {
        [JsonPropertyName("customProperty")]
        public string CustomProperty { get; set; }

        [JsonPropertyName("guid")]
        public string Guid { get; set; }
    }
}
