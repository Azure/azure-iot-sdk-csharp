// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Samples
{
    internal class ThermostatTwin : BasicDigitalTwin
    {
        [JsonPropertyName("$metadata")]
        public new ThermostatMetadata Metadata { get; set; }

        [JsonPropertyName("maxTempSinceLastReboot")]
        public double? MaxTempSinceLastReboot { get; set; }

        [JsonPropertyName("targetTemperature")]
        public double? TargetTemperature { get; set; }
    }

    internal class ThermostatMetadata : DigitalTwinMetadata
    {
        [JsonPropertyName("maxTempSinceLastReboot")]
        public ReportedPropertyMetadata MaxTempSinceLastReboot { get; set; }

        [JsonPropertyName("targetTemperature")]
        public WritableProperty TargetTemperature { get; set; }
    }

    internal class ReportedPropertyMetadata
    {
        [JsonPropertyName("lastUpdateTime")]
        public DateTimeOffset LastUpdatedOnUtc { get; set; }
    }
}
