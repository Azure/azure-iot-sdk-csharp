// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    internal class ImportError
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorStatus")]
        public string ErrorStatus { get; set; }
    }
}
