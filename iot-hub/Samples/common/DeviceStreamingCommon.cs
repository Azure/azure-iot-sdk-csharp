// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples.Common
{
    public static class DeviceStreamingCommon
    {
        /// <summary>
        /// Creates a ClientWebSocket with the proper authorization header for Device Streaming.
        /// </summary>
        /// <param name="url">Url to the Streaming Gateway.</param>
        /// <param name="authorizationToken">Authorization token to connect to the Streaming Gateway.</param>
        /// <param name="cancellationToken">The token used for cancelling this operation if desired.</param>
        /// <returns>A ClientWebSocket instance connected to the Device Streaming gateway, if successful.</returns>
        public static async Task<ClientWebSocket> GetStreamingClientAsync(Uri url, string authorizationToken, CancellationToken cancellationToken)
        {
            var wsClient = new ClientWebSocket();
            wsClient.Options.SetRequestHeader("Authorization", "Bearer " + authorizationToken);

            await wsClient.ConnectAsync(url, cancellationToken);

            return wsClient;
        }
    }
}
