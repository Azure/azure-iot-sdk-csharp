// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Protocol;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// An MQTT "will" message to be sent by this client before the client disconnects.
    /// </summary>
    public class WillMessage : IWillMessage
    {
        /// <inheritdoc />
        public byte[] Payload { get; set; }

        /// <inheritdoc />
        public MqttQualityOfServiceLevel QualityOfService { get; set; }
    }
}
