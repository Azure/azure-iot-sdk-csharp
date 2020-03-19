// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Codecs.Mqtt.Packets;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// An MQTT "will" message, used when a client disconnects
    /// </summary>
    public class WillMessage : IWillMessage
    {
        /// <inheritdoc />
        public Message Message { get; private set; }

        /// <inheritdoc />
        public QualityOfService QoS { get; set; }

        /// <summary>
        /// Creates an instance of WillMessage
        /// </summary>
        /// <param name="qos">The quality of service</param>
        /// <param name="message">The message to be sent</param>
        public WillMessage(QualityOfService qos, Message message)
        {
            QoS = qos;
            Message = message;
        }
    }
}
