// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class HumidityRange
    {
        // Properties have multiple json property annotations just for demonstration of
        // "old" API usage in the sample - TwinCollection is tightly coupled with NewtonSoft.Json
        // This is not something that is expected for test/ prod scenarios.
        [JsonPropertyName("maxHumidity")]
        [Newtonsoft.Json.JsonProperty("maxHumidity")]
        public double MaxTemperature { get; set; }

        // Properties have multiple json property annotations just for demonstration of
        // "old" API usage in the sample - TwinCollection is tightly coupled with NewtonSoft.Json
        // This is not something that is expected for test/ prod scenarios.
        [JsonPropertyName("minHumidity")]
        [Newtonsoft.Json.JsonProperty("minHumidity")]
        public double MinTemperature { get; set; }
    }
}
