// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Defines the transport fall-back types for AMQP and MQTT.
    /// </summary>
    public enum TransportFallbackType
    {
        /// <summary>
        /// The transport will fall-back to corresponding websocket if tcp connection fails.
        /// </summary>
        TcpWithWebSocketFallback = 0,

        /// <summary>
        /// WebSocket only connection with no fall-back.
        /// </summary>
        WebSocketOnly = 1,

        /// <summary>
        /// Tcp only connection with no fall-back.
        /// </summary>
        TcpOnly = 2,
    }
}
