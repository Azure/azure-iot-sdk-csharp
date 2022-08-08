// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
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
