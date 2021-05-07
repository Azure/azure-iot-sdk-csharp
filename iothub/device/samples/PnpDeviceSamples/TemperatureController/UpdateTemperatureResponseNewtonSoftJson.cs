// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class UpdateTemperatureResponseNewtonSoftJson
    {
        [JsonProperty("targetTemp")]
        public double TargetTemperature { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}
