﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The protocol over which a transport (i.e., MQTT, AMQP) communicates.
    /// </summary>
    public enum IotHubClientTransportProtocol
    {
        /// <summary>
        /// Communicate over TCP using the default port of the transport.
        /// </summary>
        /// <remarks>
        /// For MQTT, this is 8883.
        /// For AMQP, this is 5671.
        /// </remarks>
        Tcp,

        /// <summary>
        /// Communicate over web socket using port 443.
        /// </summary>
        WebSocket,
    }
}