// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The options for acknowledging a cloud-to-device (C2D) message.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>
    public enum MessageAcknowledgement
    {
        /// <summary>
        /// The message will be positively acknowledged.
        /// </summary>
        /// <remarks>
        /// This removes the message from the queue; it will not be sent again.
        /// </remarks>
        Complete,

        /// <summary>
        /// The message will be re-queued to be sent again.
        /// </summary>
        /// <remarks>
        /// This option is not supported over MQTT.
        /// </remarks>
        Abandon,

        /// <summary>
        /// The message will be negatively acknowledged.
        /// </summary>
        /// <remarks>
        /// This removes the message from the queue; it will not be sent again.
        /// <para>
        /// This option is not supported over MQTT.
        /// </para>
        /// </remarks>
        Reject,
    };
}
