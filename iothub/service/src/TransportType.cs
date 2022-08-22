// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport types supported by MessagingClient, FileUploadNotificationProcessorClient and MessageFeedbackProcessorClient.
    /// </summary>
    /// <remarks>
    /// Amqp and Amqp over WebSocket.
    /// </remarks>
    public enum TransportType
    {
        /// <summary>
        /// Advanced Message Queuing Protocol transport.
        /// </summary>
        /// <remarks>
        /// Communicate over port 5671.
        /// </remarks>
        Amqp,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over WebSocket.
        /// </summary>
        /// <remarks>
        /// Communicate over web socket using port 443.
        /// </remarks>
        Amqp_WebSocket
    }
}
