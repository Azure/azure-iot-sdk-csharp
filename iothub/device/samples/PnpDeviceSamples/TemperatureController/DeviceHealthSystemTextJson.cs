// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceHealthNewtonSystemText
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("runningTimeSeconds")]
        public double RunningTimeInSeconds { get; set; }

        [JsonPropertyName("isStopRequested")]
        public bool IsStopRequested { get; set; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}
