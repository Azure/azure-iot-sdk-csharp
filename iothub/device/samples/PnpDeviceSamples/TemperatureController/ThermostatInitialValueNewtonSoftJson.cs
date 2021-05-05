// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ThermostatInitialValueNewtonSoftJson
    {
        [JsonProperty("temp")]
        public double Temperature { get; set; }

        [JsonProperty("humidity")]
        public double Humidity { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ThermostatInitialValueNewtonSoftJson);
        }

        private bool Equals(ThermostatInitialValueNewtonSoftJson objectToCompare)
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
