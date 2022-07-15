// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Samples.Common;
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class DeviceStreamSample
    {
        private ServiceClient _serviceClient;
        private String _deviceId;

        public DeviceStreamSample(ServiceClient deviceClient, String deviceId)
        {
            _serviceClient = deviceClient;
            _deviceId = deviceId;
        }
        
        public async Task RunSampleAsync()
        {
            try
            {
                DeviceStreamRequest deviceStreamRequest = new DeviceStreamRequest(
                    streamName: "TestStream"
                );

                DeviceStreamResponse result = await _serviceClient.CreateStreamAsync(_deviceId, deviceStreamRequest).ConfigureAwait(false);

                Console.WriteLine("Stream response received: Name={0} IsAccepted={1}", deviceStreamRequest.StreamName, result.IsAccepted);

                if (result.IsAccepted)
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                    using (var stream = await DeviceStreamingCommon.GetStreamingClientAsync(result.Url, result.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                    {
                        byte[] sendBuffer = Encoding.UTF8.GetBytes("Streaming data over a stream...");
                        byte[] receiveBuffer = new byte[1024];

                        await stream.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, cancellationTokenSource.Token).ConfigureAwait(false);

                        Console.WriteLine("Sent stream data: {0}", Encoding.UTF8.GetString(sendBuffer, 0, sendBuffer.Length));

                        var receiveResult = await stream.ReceiveAsync(receiveBuffer, cancellationTokenSource.Token).ConfigureAwait(false);

                        Console.WriteLine("Received stream data: {0}", Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count));
                    }
                }
                else
                {
                    Console.WriteLine("Stream request was rejected by the device");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got an exception: {0}", ex);
                throw;
            }
        }
    }
}
