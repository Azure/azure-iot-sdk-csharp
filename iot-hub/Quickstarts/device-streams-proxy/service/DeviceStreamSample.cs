// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Samples.Common;
using System;
using System.Net;
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
        private int _localPort;

        public DeviceStreamSample(ServiceClient deviceClient, String deviceId, int localPort)
        {
            _serviceClient = deviceClient;
            _deviceId = deviceId;
            _localPort = localPort;
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] receiveBuffer = new byte[10240];

            while (localStream.CanRead)
            {
                var receiveResult = await remoteStream.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);

                await localStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
            {
                int receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async void HandleIncomingConnectionsAndCreateStreams(string deviceId, ServiceClient serviceClient, TcpClient tcpClient)
        {
            DeviceStreamRequest deviceStreamRequest = new DeviceStreamRequest(
                streamName: "TestStream"
            );

            using (var localStream = tcpClient.GetStream())
            {
                DeviceStreamResponse result = await serviceClient.CreateStreamAsync(deviceId, deviceStreamRequest, CancellationToken.None).ConfigureAwait(false);

                Console.WriteLine($"Stream response received: Name={deviceStreamRequest.StreamName} IsAccepted={result.IsAccepted}");

                if (result.IsAccepted)
                {
                    try
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(0, 1, 0)))
                        using (var remoteStream = await DeviceStreamingCommon.GetStreamingClientAsync(result.Url, result.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                        {
                            Console.WriteLine("Starting streaming");

                            await Task.WhenAny(
                                HandleIncomingDataAsync(localStream, remoteStream, cancellationTokenSource.Token),
                                HandleOutgoingDataAsync(localStream, remoteStream, cancellationTokenSource.Token)).ConfigureAwait(false);
                        }

                            Console.WriteLine("Done streaming");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Got an exception: {0}", ex);
                    }
                }
            }
            tcpClient.Close();
        }

        public async Task RunSampleAsync()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, _localPort);
            tcpListener.Start();

            while (true)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

                HandleIncomingConnectionsAndCreateStreams(_deviceId, _serviceClient, tcpClient);
            }
        }
    }
}
