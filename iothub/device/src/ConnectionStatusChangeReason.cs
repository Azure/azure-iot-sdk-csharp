// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The reason for a connection status change from a connection change event.
    /// </summary>
    public enum ConnectionStatusChangeReason
    {
        /// <summary>
        /// The client has not been opened or has been closed gracefully.
        /// </summary>
        /// <remarks>
        /// To perform more operations on the client, call <see cref="IDisposable.Dispose()"/> and then re-initialize the client.
        /// </remarks>
        ClientClosed,

        /// <summary>
        /// The client is connected and ready to perform device operations.
        /// </summary>
        ConnectionOk,

        /// <summary>
        /// When a device encounters a connection error, subsequent operations on the client will fail until conneciton
        /// has been re-established.
        /// </summary>
        /// <remarks>
        /// If the connection status is <see cref="ConnectionStatus.DisconnectedRetrying"/>, the client will try to reopen
        /// the connection. Do NOT close or open the client instance.
        /// Once the client successfully reports <see cref="ConnectionStatus.Connected"/>,
        /// operations on the client may resume.
        /// <para>
        /// If the connection status is <see cref="ConnectionStatus.Disconnected"/>, a non-retryable error occurred.
        /// To perform more operations on the client, call <see cref="IDisposable.Dispose()"/> and then re-initialize the client.
        /// </para>
        /// </remarks>
        CommunicationError,

        /// <summary>
        /// The client was disconnected due to a transient exception and the retry policy expired.
        /// </summary>
        /// <remarks>
        /// To perform more operations on the client, call <see cref="IDisposable.Dispose()"/> and then re-initialize the client.
        /// </remarks>
        RetryExpired,

        /// <summary>
        /// Incorrect credentials were supplied to the client instance.
        /// </summary>
        /// <remarks>
        /// The supplied credentials need to be updated, or a secondary shared access key might be used.
        /// </remarks>
        BadCredential,

        /// <summary>
        /// The device/module has been deleted or marked as disabled in the IoT hub instance.
        /// </summary>
        /// <remarks>
        /// Manually intervention may be required to enable or add device/module to IoT hub.
        /// </remarks>
        DeviceDisabled,
    }
}
