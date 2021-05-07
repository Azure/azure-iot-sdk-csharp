// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class UpdateTemperatureRequestSystemTextJson
    {
        [JsonPropertyName("targetTemp")]
        public double TargetTemperature { get; set; }

        [JsonPropertyName("delay")]
        public int Delay { get; set; }
    }
}
