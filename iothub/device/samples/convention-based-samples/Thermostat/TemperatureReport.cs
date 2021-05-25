// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class TemperatureReport
    {
        [JsonProperty("maxTemp")]
        public double MaximumTemperature { get; set; }

        [JsonProperty("minTemp")]
        public double MinimumTemperature { get; set; }

        [JsonProperty("avgTemp")]
        public double AverageTemperature { get; set; }

        [JsonProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}
