// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class HumidityRangeNewtonSoftJson
    {
        [JsonProperty("maxHumidity")]
        public double MaxTemperature { get; set; }

        [JsonProperty("minHumidity")]
        public double MinTemperature { get; set; }
    }
}
