// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Data included in an IoT hub event notification.
    /// </summary>
    public class DeviceEventProperties
    {
        /// <summary>
        /// The name of the IoT hub that generated the event.
        /// </summary>
        [JsonPropertyName("hubName")]
        public string HubName { get; set; }

        /// <summary>
        /// The Id of the device that generated the event.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The Id of the module that generated the event.
        /// </summary>
        [JsonPropertyName("moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// The type of operation published.
        /// </summary>
        [JsonPropertyName("opType")]
        public DeviceEventOperationType OperationType { get; set; }

        /// <summary>
        /// The date and time of the operation in UTC.
        /// </summary>
        [JsonPropertyName("operationTimestamp")]
        public DateTimeOffset OperationOnUtc { get; set; }

        /// <summary>
        /// The version of the metadata.
        /// </summary>
        [JsonPropertyName("iothub-message-schema")]
        public string MessageSchema{ get; set; }
    }
}
