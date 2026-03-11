// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class ThermostatTwin
    {
        [JsonProperty("$dtId")]
        public string Id { get; set; }

        [JsonProperty("$metadata")]
        public ThermostatMetadata Metadata { get; set; }

        [JsonProperty("maxTempSinceLastReboot")]
        public double MaxTempSinceLastReboot { get; set; }

        [JsonProperty("targetTemperature")]
        public double TargetTemperature { get; set; }
    }

    internal class ThermostatMetadata
    {
        [JsonProperty("$model")]
        public string ModelId { get; set; }

        [JsonProperty("maxTempSinceLastReboot")]
        public ReportedPropertyMetadata MaxTempSinceLastReboot { get; set; }

        [JsonProperty("targetTemperature")]
        public WritableProperty TargetTemperature { get; set; }
    }

    internal class ReportedPropertyMetadata
    {
        [JsonProperty("lastUpdateTime")]
        public DateTimeOffset LastUpdateTime { get; set; }
    }
}
