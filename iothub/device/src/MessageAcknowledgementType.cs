// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static System.Net.WebRequestMethods;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The options for acknowledging a cloud to device message.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>
    public enum MessageAcknowledgementType
    {
        /// <summary>
        /// The message will be positively acknowledged. This removes the message from the queue
        /// and it will not be sent again.
        /// </summary>
        Complete,

        /// <summary>
        /// The message will be re-added to the queue and will be sent again.
        /// </summary>
        /// <remarks>
        /// This option is not supported over MQTT or MQTT websockets
        /// </remarks>
        Abandon,

        /// <summary>
        /// The message will be negatively acknowledged. This removes the message from the queue
        /// and it will not be sent again.
        /// </summary>
        /// <remarks>
        /// This option is not supported over MQTT or MQTT websockets
        /// </remarks>
        Reject,
    };
}
