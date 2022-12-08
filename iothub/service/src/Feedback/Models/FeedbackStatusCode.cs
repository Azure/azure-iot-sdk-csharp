// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the various feedback status codes for a cloud-to-device message sent to a device.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FeedbackStatusCode
    {
        /// <summary>
        /// Indicates that the cloud-to-device message was successfully delivered to the device.
        /// </summary>
        Success,

        /// <summary>
        /// Indicates that the cloud-to-device message expired before it could be delivered to the device.
        /// </summary>
        Expired,

        /// <summary>
        /// Indicates that the cloud-to-device message has been placed in a dead-lettered state.
        /// </summary>
        /// <remarks>
        /// This happens when the message reaches the maximum count for the number of times it can transition between enqueued and invisible states.
        /// </remarks>
        DeliveryCountExceeded,

        /// <summary>
        /// Indicates that the cloud-to-device message was rejected by the device.
        /// </summary>
        Rejected,

        /// <summary>
        /// Indicates that the cloud-to-device message was purged from IoT hub.
        /// </summary>
        Purged
    }
}
