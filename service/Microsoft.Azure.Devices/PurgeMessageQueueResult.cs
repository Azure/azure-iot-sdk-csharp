// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using Newtonsoft.Json;

    /// <summary>
    /// Result of a device message queue purge operation.
    /// </summary>
    public sealed class PurgeMessageQueueResult
    {
        /// <summary>
        /// The ID of the device whose messages are being purged.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", Required = Required.Always)]
        public string DeviceId { get; set; }

        /// <summary>
        /// The total number of messages that were purged from the device's queue.
        /// </summary>
        [JsonProperty(PropertyName = "totalMessagesPurged", Required = Required.Always)]
        public int TotalMessagesPurged;
    }
}
