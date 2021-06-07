// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class TemperatureReport
    {
        [JsonPropertyName("maxTemp")]
        public double MaximumTemperature { get; set; }

        [JsonPropertyName("minTemp")]
        public double MinimumTemperature { get; set; }

        [JsonPropertyName("avgTemp")]
        public double AverageTemperature { get; set; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}
