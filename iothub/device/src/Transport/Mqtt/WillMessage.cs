// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// An MQTT "will" message, used when a client disconnects
    /// </summary>
    public class WillMessage : IWillMessage
    {
        /// <inheritdoc />
        public byte[] Payload { get; private set; }

        /// <inheritdoc />
        public QualityOfService QualityOfService { get; set; }

        /// <summary>
        /// Creates an instance of WillMessage
        /// </summary>
        /// <param name="qualityOfService">The quality of service</param>
        /// <param name="payload">The payload to be sent in the will message</param>
        public WillMessage(QualityOfService qualityOfService, byte[] payload)
        {
            QualityOfService = qualityOfService;
            Payload = payload;
        }
    }
}
