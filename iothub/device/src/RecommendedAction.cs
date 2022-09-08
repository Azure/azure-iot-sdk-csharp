// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The recommended action for device applications to take in response to a connection change event.
    /// </summary>
    public enum RecommendedAction
    {
        /// <summary>
        /// It's recommended to initialize (if previously open, dispose, and then call open) the device client when the client is not connected.
        /// </summary>
        /// <remarks>
        /// When a client is first initialized, this is the default state.
        /// When a client is disconnected with retries being exhausted or non-retryable errors, the client also returns the decision on whether to attempt reconnection to
        /// the device app, with reasons such as:
        /// <list type="bullet">
        /// <item><see cref="ConnectionStatusChangeReason.RetryExpired"/></item>
        /// <item><see cref="ConnectionStatusChangeReason.CommunicationError"/></item>
        /// </list>
        /// </remarks>
        OpenConnection,

        /// <summary>
        /// It's recommended to perform operations normally on your device client as it is successfully connected to the IoT hub.
        /// </summary>
        PerformNormally,

        /// <summary>
        /// It's recommended to not perform any operations on the client while it is trying to reconnect.
        /// </summary>
        /// <remarks>
        /// This occurs when the client has encountered a retryable error. The connection status is <see cref="ConnectionStatus.DisconnectedRetrying"/>.
        /// </remarks>
        WaitForRetryPolicy,

        /// <summary>
        /// This is a terminal state of the client where it is unclear if the device will ever be able to connect, and may require manual intervention.
        /// </summary>
        /// <remarks>
        /// This can occur when the client has been closed gracefully (by calling <see cref="InternalClient2.CloseAsync(CancellationToken)"/>.
        /// Other terminal states include when the client has been disconnected due to non-retryable exceptions. The disconnection reasons include:
        /// <list type="bullet">
        /// <item><see cref="ConnectionStatusChangeReason.BadCredential"/></item>
        /// <item><see cref="ConnectionStatusChangeReason.DeviceDisabled"/></item>
        /// </list>
        /// </remarks>
        Quit,
    }
}
