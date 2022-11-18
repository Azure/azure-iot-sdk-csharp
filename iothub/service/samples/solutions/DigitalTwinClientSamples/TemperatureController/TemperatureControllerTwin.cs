// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Samples
{
    internal class TemperatureControllerTwin : BasicDigitalTwin
    {
        [JsonPropertyName("$metadata")]
        public new TemperatureControllerMetadata Metadata { get; set; }

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("thermostat1")]
        public ThermostatTwin Thermostat1 { get; set; }

        [JsonPropertyName("thermostat2")]
        public ThermostatTwin Thermostat2 { get; set; }
    }

    internal class TemperatureControllerMetadata : DigitalTwinMetadata
    {
        [JsonPropertyName("serialNumber")]
        public ReportedPropertyMetadata SerialNumber { get; set; }

        [JsonPropertyName("thermostat1")]
        public WritableProperty Thermostat1 { get; set; }

        [JsonPropertyName("thermostat2")]
        public WritableProperty Thermostat2 { get; set; }
    }
}