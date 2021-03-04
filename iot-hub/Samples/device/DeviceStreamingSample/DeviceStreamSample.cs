// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Samples.Common;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceStreamSample
    {
        private readonly DeviceClient _deviceClient;

        public DeviceStreamSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        public async Task RunSampleAsync(bool acceptDeviceStreamingRequest = true)
        {
            var buffer = new byte[1024];

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            DeviceStreamRequest streamRequest = await _deviceClient.WaitForDeviceStreamRequestAsync(cts.Token);

            if (streamRequest != null)
            {
                if (!acceptDeviceStreamingRequest)
                {
                    await _deviceClient.RejectDeviceStreamRequestAsync(streamRequest, cts.Token);
                }
                else
                {
                    await _deviceClient.AcceptDeviceStreamRequestAsync(streamRequest, cts.Token);

                    using ClientWebSocket webSocket = await DeviceStreamingCommon.GetStreamingClientAsync(
                        streamRequest.Uri,
                        streamRequest.AuthorizationToken,
                        cts.Token);

                    WebSocketReceiveResult receiveResult = await webSocket
                        .ReceiveAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), cts.Token);
                    Console.WriteLine("Received stream data: {0}", Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                        WebSocketMessageType.Binary,
                        true,
                        cts.Token);
                    Console.WriteLine("Sent stream data: {0}", Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));

                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
                }
            }
        }
    }
}
