// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class TemperatureControllerTwin
    {
        [JsonPropertyName("$dtId")]
        public string Id { get; set; }

        [JsonPropertyName("$metadata")]
        public TemperatureControllerMetadata Metadata { get; set; }

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("thermostat1")]
        public Thermostat Thermostat1 { get; set; }

        [JsonPropertyName("thermostat2")]
        public Thermostat Thermostat2 { get; set; }
    }

    internal class TemperatureControllerMetadata
    {
        [JsonPropertyName("$model")]
        public string ModelId { get; set; }

        [JsonPropertyName("serialNumber")]
        public ReportedPropertyMetadata SerialNumber { get; set; }
    }

    internal class Thermostat
    {
        [JsonPropertyName("$metadata")]
        public ThermostatComponentMetadata Metadata { get; set; }

        [JsonPropertyName("maxTempSinceLastReboot")]
        public double MaxTempSinceLastReboot { get; set; }

        [JsonPropertyName("targetTemperature")]
        public double TargetTemperature { get; set; }
    }

    internal class ThermostatComponentMetadata
    {
        [JsonPropertyName("maxTempSinceLastReboot")]
        public ReportedPropertyMetadata MaxTempSinceLastReboot { get; set; }

        [JsonPropertyName("targetTemperature")]
        public WritableProperty TargetTemperature { get; set; }
    }
}
