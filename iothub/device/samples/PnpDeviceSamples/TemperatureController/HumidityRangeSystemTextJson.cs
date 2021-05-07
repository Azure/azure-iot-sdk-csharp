// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class HumidityRangeSystemTextJson
    {
        [JsonPropertyName("maxHumidity")]
        public double MaxTemperature { get; set; }

        [JsonPropertyName("minHumidity")]
        public double MinTemperature { get; set; }
    }
}
