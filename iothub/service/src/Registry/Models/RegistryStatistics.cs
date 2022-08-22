// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The device registry statistics that can be retrieved from IoT hub.
    /// </summary>
    public class RegistryStatistics
    {
        /// <summary>
        /// Gets or sets the count of all devices.
        /// </summary>
        [JsonProperty(PropertyName = "totalDeviceCount")]
        public long TotalDeviceCount { get; set; }

        /// <summary>
        /// Gets or sets the count of all enabled devices.
        /// </summary>
        [JsonProperty(PropertyName = "enabledDeviceCount")]
        public long EnabledDeviceCount { get; set; }

        /// <summary>
        /// Gets or sets the count of all disabled devices.
        /// </summary>
        [JsonProperty(PropertyName = "disabledDeviceCount")]
        public long DisabledDeviceCount { get; set; }
    }
}
