// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal enum ClientTransportStatus
    {
        /// <summary>
        /// Represents the state when the client (transport) is closed.
        /// </summary>
        /// <remarks>
        /// This state can be reached through the following transitions:
        ///     <list type="bullet">
        ///         <item><description>The initial state when the client is initialized.</description></item>
        ///         <item><description><see cref="Opening"/> to <see cref="Closed"/>: <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/>
        ///             is called but the call doesn't complete successfully.</description></item>
        ///         <item><description><see cref="Opening"/> to <see cref="Closed"/>: when the connection is lost and the subsequent reconnection attempt fails.
        ///             This is handled by code in <see cref="ConnectionStatusHandler.HandleDisconnectAsync()"/> which is separate from the regular <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/> flow.</description></item>
        ///         <item><description><see cref="Open"/> to <see cref="Closed"/>: when the connection is lost after the client had been successfully opened..</description></item>
        ///         <item><description><see cref="Closing"/> to <see cref="Closed"/>: <see cref="IDelegatingHandler.CloseAsync(CancellationToken)"/> completes successfully.</description></item>
        ///         <item><description><see cref="Closing"/> to <see cref="Closed"/>: <see cref="DefaultDelegatingHandler.Dispose()"/> completes successfully.</description></item>
        ///     </list>
        /// </remarks>
        Closed,

        /// <summary>
        /// Represents the state when the client (transport) is attempting to open.
        /// </summary>
        /// <remarks>
        /// This state can be reached through the following transitions:
        ///     <list type="bullet">
        ///         <item><description><see cref="Closed"/> to <see cref="Opening"/>: <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/>
        ///             is called and the client is attempting to open.</description></item>
        ///         <item><description><see cref="Closed"/> to <see cref="Opening"/>: when the connection is lost and the client is attempting to reconnect.
        ///             This is handled by code in <see cref="ConnectionStatusHandler.HandleDisconnectAsync()"/> which is separate from the regular <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/> flow.</description></item>
        ///     </list>
        /// </remarks>
        Opening,

        /// <summary>
        /// Represents the state when the client (transport) is open.
        /// </summary>
        /// <remarks>
        /// This state can be reached through the following transitions:
        ///     <list type="bullet">
        ///         <item><description><see cref="Opening"/> to <see cref="Open"/>: <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/> completes successfully.</description></item>
        ///         <item><description><see cref="Opening"/> to <see cref="Open"/>: when the connection is lost and then client successfully reconnects.
        ///             This is handled by code in <see cref="ConnectionStatusHandler.HandleDisconnectAsync()"/> which is separate from the regular <see cref="IDelegatingHandler.OpenAsync(CancellationToken)"/> flow.</description></item>
        ///     </list>
        /// </remarks>
        Open,

        /// <summary>
        /// Represents the state when the client (transport) is open.
        /// </summary>
        /// <remarks>
        /// This state can be reached through the following transitions:
        ///     <list type="bullet">
        ///         <item><description>Any state but <see cref="Closed"/> to <see cref="Closing"/>: <see cref="IDelegatingHandler.CloseAsync(CancellationToken)"/>
        ///             is called and the client is attempting to close.</description></item>
        ///         <item><description>Any state to <see cref="Closing"/>: <see cref="DefaultDelegatingHandler.Dispose()"/>
        ///             is called and the client is attempting to dispose.</description></item>
        ///     </list>
        /// </remarks>
        Closing,
    }
}