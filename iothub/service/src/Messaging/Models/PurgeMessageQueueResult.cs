// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Result of a device message queue purge operation.
    /// </summary>
    public sealed class PurgeMessageQueueResult
    {
        /// <summary>
        /// Initializes an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        /// <param name="deviceId">
        /// The Id of the device whose messages are being purged.
        /// </param>
        /// <param name="totalMessagesPurged">
        /// The total number of messages that were purged from the device's queue.
        /// </param>
        public PurgeMessageQueueResult(string deviceId, int totalMessagesPurged)
        {
            DeviceId = deviceId;
            TotalMessagesPurged = totalMessagesPurged;
        }

        /// <summary>
        /// The Id of the device whose messages are being purged.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", Required = Required.Always)]
        public string DeviceId { get; set; }

        /// <summary>
        /// The total number of messages that were purged from the device's queue.
        /// </summary>
        [JsonProperty(PropertyName = "totalMessagesPurged", Required = Required.Always)]
        public int TotalMessagesPurged { get; set; }
    }
}
