// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Status of capabilities enabled on the device.
    /// </summary>
    public class ProvisioningTwinCapabilities
    {
        /// <summary>
        /// IoT Edge capability.
        /// </summary>
        [JsonPropertyName("iotEdge")]
        public bool IsIotEdge { get; set; }
    }
}
