// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Delegate for connection state changed.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="IotHubDeviceClient"/> and <see cref="IotHubModuleClient"/>.
    /// </remarks>
    /// <param name="state">The updated connection state</param>
    /// <param name="reason">The reason for the connection state change</param>
    public delegate void ConnectionStateChangeHandler(ConnectionState state, ConnectionStateChangeReason reason);

    /// <summary>
    /// Delegate for method call. This will be called every time we receive a method call that was registered.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="IotHubDeviceClient"/> and <see cref="IotHubModuleClient"/>.
    /// </remarks>
    /// <param name="methodRequest">Class with details about method.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MethodResponse</returns>
    public delegate Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext);

    /// <summary>
    /// Delegate for desired property update callbacks. This will be called every time we receive a patch from the service.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="IotHubDeviceClient"/> and <see cref="IotHubModuleClient"/>.
    /// </remarks>
    /// <param name="desiredProperties">Properties that were contained in the update that was received from the service</param>
    /// <param name="userContext">Context object passed in when the callback was registered</param>
    public delegate Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="IotHubDeviceClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="IotHubDeviceClient.SetReceiveMessageHandlerAsync(ReceiveMessageCallback, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    public delegate Task ReceiveMessageCallback(Message message, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="IotHubModuleClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="IotHubModuleClient.SetInputMessageHandlerAsync(string, MessageHandler, object, CancellationToken)"/>
    /// and <see cref="IotHubModuleClient.SetMessageHandlerAsync(MessageHandler, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MessageResponse</returns>
    public delegate Task<MessageResponse> MessageHandler(Message message, object userContext);
}
