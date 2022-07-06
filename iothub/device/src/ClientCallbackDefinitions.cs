﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Delegate for connection status changed.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="status">The updated connection status</param>
    /// <param name="reason">The reason for the connection status change</param>
    public delegate void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason);

    /// <summary>
    /// Delegate for method call. This will be called every time we receive a method call that was registered.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="methodRequest">Class with details about method.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MethodResponse</returns>
    public delegate Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext);

    /// <summary>
    /// Delegate for desired property update callbacks. This will be called every time we receive a patch from the service.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="desiredProperties">Properties that were contained in the update that was received from the service</param>
    /// <param name="userContext">Context object passed in when the callback was registered</param>
    public delegate Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="DeviceClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="DeviceClient.SetReceiveMessageHandlerAsync(ReceiveMessageCallback, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    public delegate Task ReceiveMessageCallback(Message message, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="ModuleClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="ModuleClient.SetInputMessageHandlerAsync(string, MessageHandler, object, CancellationToken)"/>
    /// and <see cref="ModuleClient.SetMessageHandlerAsync(MessageHandler, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MessageResponse</returns>
    public delegate Task<MessageResponse> MessageHandler(Message message, object userContext);
}
