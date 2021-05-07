// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ThermostatInitialValueSystemTextJson
    {
        [JsonPropertyName("temp")]
        public double Temperature { get; set; }

        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ThermostatInitialValueSystemTextJson);
        }

        private bool Equals(ThermostatInitialValueSystemTextJson objectToCompare)
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
