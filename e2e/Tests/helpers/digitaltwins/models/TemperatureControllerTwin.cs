// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class TemperatureControllerTwin
    {
        [JsonProperty("$dtId")]
        public string Id { get; set; }

        [JsonProperty("$metadata")]
        public TemperatureControllerMetadata Metadata { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("thermostat1")]
        public Thermostat Thermostat1 { get; set; }

        [JsonProperty("thermostat2")]
        public Thermostat Thermostat2 { get; set; }
    }

    internal class TemperatureControllerMetadata
    {
        [JsonProperty("$model")]
        public string ModelId { get; set; }

        [JsonProperty("serialNumber")]
        public ReportedPropertyMetadata SerialNumber { get; set; }
    }

    internal class Thermostat
    {
        [JsonProperty("$metadata")]
        public ThermostatComponentMetadata Metadata { get; set; }

        [JsonProperty("maxTempSinceLastReboot")]
        public double MaxTempSinceLastReboot { get; set; }

        [JsonProperty("targetTemperature")]
        public double TargetTemperature { get; set; }
    }

    internal class ThermostatComponentMetadata
    {
        [JsonProperty("maxTempSinceLastReboot")]
        public ReportedPropertyMetadata MaxTempSinceLastReboot { get; set; }

        [JsonProperty("targetTemperature")]
        public WritableProperty TargetTemperature { get; set; }
    }
}
