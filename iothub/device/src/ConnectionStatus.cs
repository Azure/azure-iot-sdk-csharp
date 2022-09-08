// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The connection status from a connection change event.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// The device or module is disconnected.
        /// </summary>
        /// <remarks>
        /// Observe the associated <see cref="RecommendedAction"/> or <see cref="ConnectionStatusChangeReason"/> (and any exception thrown), and take appropriate action.
        /// </remarks>
        Disconnected,

        /// <summary>
        /// The device or module is connected.
        /// </summary>
        /// <remarks>The client is connected, and ready to be used.</remarks>
        Connected,

        /// <summary>
        /// The client is attempting to reconnect per retry policy.
        /// </summary>
        /// <remarks>The client is attempting to recover the connection. Do NOT close or open the client instance when it is retrying.</remarks>
        DisconnectedRetrying,

        /// <summary>
        /// The device connection was closed gracefully.
        /// </summary>
        /// <remarks>
        /// To perform more operations on the client, call <see cref="InternalClient.Dispose()"/> and then re-initialize the client.
        /// </remarks>
        Closed,
    }
}
