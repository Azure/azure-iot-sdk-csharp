// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Status of capabilities enabled on the device.
    /// </summary>
    public sealed class ClientCapabilities
    {
        /// <summary>
        /// Indicates if the device is an IoT Edge device.
        /// </summary>
        [JsonPropertyName("iotEdge")]
        public bool IsIotEdge { get; set; }
    }
}
