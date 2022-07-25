// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Transport types supported by the device and module clients - AMQP/TCP, HTTP 1.1, MQTT/TCP, AMQP/WS, MQTT/WS
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "Public facing types cannot be renamed. This is considered a breaking change")]
    public enum TransportType
    {
        /// <summary>
        /// HyperText Transfer Protocol version 1 transport.
        /// </summary>
        Http = 1,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over WebSocket only.
        /// </summary>
        Amqp_WebSocket_Only = 2,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over native TCP only
        /// </summary>
        Amqp_Tcp_Only = 3,

        /// <summary>
        /// Message Queuing Telemetry Transport over Websocket only.
        /// </summary>
        Mqtt_WebSocket_Only = 5,

        /// <summary>
        /// Message Queuing Telemetry Transport over native TCP only
        /// </summary>
        Mqtt_Tcp_Only = 6,
    }
}
