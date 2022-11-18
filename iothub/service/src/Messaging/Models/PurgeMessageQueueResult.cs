// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Result of a device message queue purge operation.
    /// </summary>
    public class PurgeMessageQueueResult
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal PurgeMessageQueueResult()
        { }

        /// <summary>
        /// The Id of the device whose messages are being purged.
        /// </summary>
        [JsonPropertyName("deviceId", Required = Required.Always)]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// The total number of messages that were purged from the device's queue.
        /// </summary>
        [JsonPropertyName("totalMessagesPurged", Required = Required.Always)]
        public int TotalMessagesPurged { get; protected internal set; }
    }
}
