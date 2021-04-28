// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class DeviceHealth
    {
        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("runningTimeSeconds")]
        [JsonPropertyName("runningTimeSeconds")]
        public double RunningTimeInSeconds { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("isStopRequested")]
        [JsonPropertyName("isStopRequested")]
        public bool IsStopRequested { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("startTime")]
        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("endTime")]
        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}
