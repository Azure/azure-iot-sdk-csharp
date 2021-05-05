// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class ThermostatInitialValue
    {
        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("temp")]
        [JsonPropertyName("temp")]
        public double Temperature { get; set; }

        // Multiple json property annotations are added to demonstrate the difference in
        // API usgage for the different serializers.
        [JsonProperty("humidity")]
        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ThermostatInitialValue);
        }

        private bool Equals(ThermostatInitialValue objectToCompare)
        {
            return objectToCompare.Temperature == Temperature
                && objectToCompare.Humidity == Humidity;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
