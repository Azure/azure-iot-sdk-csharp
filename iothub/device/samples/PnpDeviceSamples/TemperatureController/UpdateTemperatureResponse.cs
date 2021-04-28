// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class UpdateTemperatureResponse
    {
        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("targetTemp")]
        [JsonPropertyName("targetTemp")]
        public double TargetTemperature { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
