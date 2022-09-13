// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// An MQTT "will" message to be sent by this client before the client disconnects.
    /// </summary>
    public interface IWillMessage
    {
        /// <summary>
        /// The payload to be sent in the will message.
        /// </summary>
        byte[] Payload { get; }

        /// <summary>
        /// An agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message
        /// </summary>
        QualityOfService QualityOfService { get; set; }
    }
}
