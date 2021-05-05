// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceHealthNewtonSoftJson
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("runningTimeSeconds")]
        public double RunningTimeInSeconds { get; set; }

        [JsonProperty("isStopRequested")]
        public bool IsStopRequested { get; set; }

        [JsonProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}
