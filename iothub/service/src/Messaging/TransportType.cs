using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport types supported by MessagingClient.
    /// </summary>
    /// <remarks>
    /// Amqp and Amqp over WebSocket.
    /// </remarks>
    public enum TransportType
    {
        /// <summary>
        /// Advanced Message Queuing Protocol transport.
        /// </summary>
        Amqp,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over WebSocket.
        /// </summary>
        Amqp_WebSocket
    }
}
