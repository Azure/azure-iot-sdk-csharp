// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Settings around reprovisioning
    /// </summary>
    public class ReprovisionPolicy
    {
        /// <summary>
        /// When set to true (default), the Device Provisioning Service will evaluate the device's IoT Hub assignment
        /// and update it if necessary for any provisioning requests beyond the first from a given device.
        /// If set to false, the device will stay assigned to its current IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "updateHubAssignment", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool UpdateHubAssignment { get; set; }

        /// <summary>
        /// When set to true (default), the Device Provisioning Service will migrate the device's data (twin, device capabilities, and device ID) from one IoT hub to another during an IoT hub assignment update.
        /// If set to false, the Device Provisioning Service will reset the device's data to the initial desired configuration stored in the provisioning service's enrollment list.
        /// </summary>
        [JsonProperty(PropertyName = "migrateDeviceData", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool MigrateDeviceData { get; set; }
    }
}