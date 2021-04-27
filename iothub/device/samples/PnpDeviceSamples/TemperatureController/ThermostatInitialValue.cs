// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class ThermostatInitialValue
    {
        // Properties have multiple json property annotations just for demonstration of
        // "old" API usage in the sample - TwinCollection is tightly coupled with NewtonSoft.Json
        // This is not something that is expected for test/ prod scenarios.
        [JsonPropertyName("temp")]
        [Newtonsoft.Json.JsonProperty("temp")]
        public double Temperature { get; set; }

        // Properties have multiple json property annotations just for demonstration of
        // "old" API usage in the sample - TwinCollection is tightly coupled with NewtonSoft.Json
        // This is not something that is expected for test/ prod scenarios.
        [JsonPropertyName("humidity")]
        [Newtonsoft.Json.JsonProperty("humidity")]
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
