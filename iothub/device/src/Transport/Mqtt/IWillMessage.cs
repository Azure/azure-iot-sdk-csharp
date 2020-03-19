// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Codecs.Mqtt.Packets;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// An MQTT "will" message, used when a client disconnects
    /// </summary>
    public interface IWillMessage
    {
        /// <summary>
        /// The message to be sent
        /// </summary>
        Message Message { get; }

        /// <summary>
        /// An agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message
        /// </summary>
        QualityOfService QoS { get; set; }
    }
}
