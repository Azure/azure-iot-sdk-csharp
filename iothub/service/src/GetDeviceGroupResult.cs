// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class GetDeviceGroupResult
    {
        [JsonProperty("DeviceGroup")]
        string DeviceGroup { get; set; }

        [JsonProperty("status")]
        int status { get; set; }

        [JsonProperty("payload")]
        internal JRaw Payload { get; set; }
    }
}