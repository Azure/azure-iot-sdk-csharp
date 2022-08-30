// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport types supported by MessagingClient, FileUploadNotificationProcessorClient and MessageFeedbackProcessorClient.
    /// </summary>
    /// <remarks>
    /// Only supports Amqp and Amqp over WebSocket.
    /// </remarks>
    public enum TransportType
    {
        /// <summary>
        /// Communicate over TCP using the port 5671.
        /// </summary>
        Tcp,

        /// <summary>
        /// Communicate over web socket using port 443.
        /// </summary>
        WebSocket
    }
}
