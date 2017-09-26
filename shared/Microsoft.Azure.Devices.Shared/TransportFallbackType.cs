// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Defines the transport fall-back types for AMQP and MQTT.
    /// </summary>
    public enum TransportFallbackType
    {
        TcpWithWebSocketFallback = 0,
        WebSocketOnly = 1,
        TcpOnly = 2,
    }
}
