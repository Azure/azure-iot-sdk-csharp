// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices.Shared
{
    using Newtonsoft.Json;

    /// <summary>
    /// Status of Capabilities enabled on the device
    /// </summary>
    public class DeviceCapabilities
    {
        [JsonProperty(PropertyName = "iotEdge")]
        public bool IotEdge { get; set; }
    }
}
#endif
