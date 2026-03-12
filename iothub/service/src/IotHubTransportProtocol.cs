// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport protocol types supported by MessagingClient, FileUploadNotificationProcessorClient and MessageFeedbackProcessorClient.
    /// </summary>
    /// <remarks>
    /// Only supports AMQP over TCP and AMQP over web socket.
    /// </remarks>
    public enum IotHubTransportProtocol
    {
        /// <summary>
        /// Communicate over TCP using port 5671.
        /// </summary>
        Tcp,

        /// <summary>
        /// Communicate over web socket using port 443.
        /// </summary>
        WebSocket
    }
}
