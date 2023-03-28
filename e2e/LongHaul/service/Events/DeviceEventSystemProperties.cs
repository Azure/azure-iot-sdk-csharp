// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Data included in an IoT hub event notification.
    /// </summary>
    public class DeviceEventSystemProperties
    {
        /// <summary>
        /// The content type of the payload.
        /// </summary>
        [JsonPropertyName("content-type")]
        public string ContentType { get; set; }

        /// <summary>
        /// The content encoding of the payload.
        /// </summary>
        [JsonPropertyName("content-encoding")]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// The Id of the device connection.
        /// </summary>
        [JsonPropertyName("iothub-connection-device-id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The date and time of the enqueued event in UTC.
        /// </summary>
        [JsonPropertyName("iothub-enqueuedtime")]
        public DateTimeOffset EnqueuedOnUtc { get; set; }

        /// <summary>
        /// The date and time of the enqueued event with the time zone information.
        /// </summary>
        [JsonPropertyName("x-opt-enqueued-time")]
        public DateTimeOffset EnqueuedOn { get; set; }

        /// <summary>
        /// The message source.
        /// </summary>
        [JsonPropertyName("iothub-message-source")]
        public DeviceEventMessageSource MessageSource { get; set; }

        /// <summary>
        /// The version of the metadata.
        /// </summary>
        [JsonPropertyName("iothub-message-schema")]
        public string MessageSchema{ get; set; }
    }
}
