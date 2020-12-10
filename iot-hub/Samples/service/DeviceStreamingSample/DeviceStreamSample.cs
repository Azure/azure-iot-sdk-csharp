// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Samples.Common;

namespace Microsoft.Azure.Devices.Samples
{
    public class DeviceStreamSample
    {
        private readonly ServiceClient _serviceClient;
        private readonly string _deviceId;

        public DeviceStreamSample(ServiceClient deviceClient, string deviceId)
        {
            _serviceClient = deviceClient;
            _deviceId = deviceId;
        }
        
        public async Task RunSampleAsync()
        {
            try
            {
                var deviceStreamRequest = new DeviceStreamRequest("TestStream");

                DeviceStreamResponse result = await _serviceClient.CreateStreamAsync(_deviceId, deviceStreamRequest);

                Console.WriteLine($"Stream response received: Name={deviceStreamRequest.StreamName} IsAccepted={result.IsAccepted}");

                if (result.IsAccepted)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                    using ClientWebSocket stream = await DeviceStreamingCommon.GetStreamingClientAsync(result.Uri, result.AuthorizationToken, cts.Token);

                    byte[] sendBuffer = Encoding.UTF8.GetBytes("Streaming data over a stream...");
                    byte[] receiveBuffer = new byte[1024];

                    await stream.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, cts.Token);
                    Console.WriteLine($"Sent stream data: {Encoding.UTF8.GetString(sendBuffer, 0, sendBuffer.Length)}");

                    var receiveResult = await stream.ReceiveAsync(receiveBuffer, cts.Token);
                    Console.WriteLine($"Received stream data: {Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count)}");

                    await stream.CloseAsync(WebSocketCloseStatus.NormalClosure, "Streaming completed", new CancellationToken());
                }
                else
                {
                    Console.WriteLine("Stream request was rejected by the device");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got an exception: {ex}");
                throw;
            }
        }
    }
}
