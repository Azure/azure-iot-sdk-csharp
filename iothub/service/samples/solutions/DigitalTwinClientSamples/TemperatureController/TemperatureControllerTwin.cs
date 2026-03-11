// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    internal class TemperatureControllerTwin : BasicDigitalTwin
    {
        [JsonProperty("$metadata")]
        public new TemperatureControllerMetadata Metadata { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("thermostat1")]
        public ThermostatTwin Thermostat1 { get; set; }

        [JsonProperty("thermostat2")]
        public ThermostatTwin Thermostat2 { get; set; }
    }

    internal class TemperatureControllerMetadata : DigitalTwinMetadata
    {
        [JsonProperty("serialNumber")]
        public ReportedPropertyMetadata SerialNumber { get; set; }

        [JsonProperty("thermostat1")]
        public WritableProperty Thermostat1 { get; set; }

        [JsonProperty("thermostat2")]
        public WritableProperty Thermostat2 { get; set; }
    }
}