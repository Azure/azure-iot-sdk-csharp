// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    [DoNotParallelize]
    public class ClientWebSocketTransportTests
    {
        private const string IotHubName = "localhost";
        private const int Port = 12346;

        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_thirtySeconds = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_fiveMinutes = TimeSpan.FromMinutes(5);

        private static HttpListener s_listener;
        private static byte[] s_byteArray = new byte[10] { 0x5, 0x6, 0x7, 0x8, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF };

        [AssemblyInitialize()]
        public static void AssembyInitialize(TestContext testcontext)
        {
            s_listener = new HttpListener();
            s_listener.Prefixes.Add($"http://+:{Port}{AmqpsConstants.UriSuffix}/");
            s_listener.Start();
            RunWebSocketServer().Fork();
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            s_listener.Stop();
        }

        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        public void ClientWebSocketTransportWriteWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            clientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        public async Task ClientWebSocketTransportReadWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            var args = new TransportAsyncCallbackArgs();
            bool isReadComplete = false;
            args.CompletedCallback = (TransportAsyncCallbackArgs args) =>
            {
                if (args.Exception != null)
                {
                    throw args.Exception;
                }

                // Verify that data matches what was sent
                if (s_byteArray.Length != args.Count)
                {
                    throw new InvalidOperationException("Expected " + s_byteArray.Length + " bytes in response");
                }

                for (int i = 0; i < args.Count; i++)
                {
                    if (s_byteArray[i] != args.Buffer[i])
                    {
                        throw new InvalidOperationException("Response contents do not match what was sent");
                    }
                }

                isReadComplete = true;
            };
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            if (clientWebSocketTransport.ReadAsync(args))
            {
                while (!isReadComplete)
                {
                    await Task.Delay(s_oneSecond);
                }
            }

            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }

        // The following tests can only be run in Administrator mode
        [TestMethod]
        [Timeout(10000)] // if it is going to fail, fail fast. Otherwise it can go on for 4+ minutes. :(
        [DoNotParallelize]
        public async Task ReadWriteTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{AmqpsConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);

            // Test Write API
            var args = new TransportAsyncCallbackArgs();
            args.CompletedCallback = (TransportAsyncCallbackArgs args) =>
            {
                if (args.BytesTransfered != s_byteArray.Length)
                {
                    throw new InvalidOperationException("All the bytes sent were not transferred");
                }

                if (args.Exception != null)
                {
                    throw args.Exception;
                }
            };
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            clientWebSocketTransport.WriteAsync(args);

            // Test Read API
            bool isReadComplete = false;
            args.CompletedCallback = (TransportAsyncCallbackArgs args) =>
            {
                if (args.Exception != null)
                {
                    throw args.Exception;
                }

                // Verify that data matches what was sent
                if (s_byteArray.Length != args.Count)
                {
                    throw new InvalidOperationException("Expected " + s_byteArray.Length + " bytes in response");
                }

                for (int i = 0; i < args.Count; i++)
                {
                    if (s_byteArray[i] != args.Buffer[i])
                    {
                        throw new InvalidOperationException("Response contents do not match what was sent");
                    }
                }

                isReadComplete = true;
            };

            if (clientWebSocketTransport.ReadAsync(args))
            {
                while (!isReadComplete)
                {
                    await Task.Delay(s_oneSecond);
                }
            }

            // Once Read operation is complete, close websocket transport
            // Test Close API
            await clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] //TODO #318
        public async Task ReadAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{AmqpsConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            await clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);

            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            args.CompletedCallback = (TransportAsyncCallbackArgs args) =>
            {
                if (args.Exception != null)
                {
                    throw args.Exception;
                }

                // Verify that data matches what was sent
                if (s_byteArray.Length != args.Count)
                {
                    throw new InvalidOperationException("Expected " + s_byteArray.Length + " bytes in response");
                }

                for (int i = 0; i < args.Count; i++)
                {
                    if (s_byteArray[i] != args.Buffer[i])
                    {
                        throw new InvalidOperationException("Response contents do not match what was sent");
                    }
                }
            };
            clientWebSocketTransport.ReadAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] //TODO #318
        public async Task WriteAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{AmqpsConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            await clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);

            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            args.CompletedCallback = (TransportAsyncCallbackArgs args) =>
            {
                if (args.BytesTransfered != s_byteArray.Length)
                {
                    throw new InvalidOperationException("All the bytes sent were not transferred");
                }

                if (args.Exception != null)
                {
                    throw args.Exception;
                }
            };
            clientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        public async Task ReadAfterAbortTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);
            Uri uri = new Uri($"ws://{IotHubName}:{Port}{AmqpsConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            clientWebSocketTransport.Abort();
            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            clientWebSocketTransport.ReadAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] //TODO #318
        public async Task WriteAfterAbortTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(AmqpsConstants.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{AmqpsConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            using var clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            clientWebSocketTransport.Abort();
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            clientWebSocketTransport.WriteAsync(args);
        }

        public static async Task RunWebSocketServer()
        {
            try
            {
                while (true)
                {
                    HttpListenerContext context = await s_listener.GetContextAsync().ConfigureAwait(false);
                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }

                    HttpListenerWebSocketContext webSocketContext = await context
                        .AcceptWebSocketAsync(AmqpsConstants.Amqpwsb10, 8 * 1024, s_fiveMinutes)
                        .ConfigureAwait(false);

                    var buffer = new byte[1 * 1024];
                    var arraySegment = new ArraySegment<byte>(buffer);
                    var cancellationToken = new CancellationToken();
                    WebSocketReceiveResult receiveResult = await webSocketContext.WebSocket
                        .ReceiveAsync(arraySegment, cancellationToken)
                        .ConfigureAwait(false);

                    // Echo the data back to the client
                    var responseCancellationToken = new CancellationToken();
                    var responseBuffer = new byte[receiveResult.Count];
                    for (int i = 0; i < receiveResult.Count; i++)
                    {
                        responseBuffer[i] = arraySegment.Array[i];
                    }

                    var responseSegment = new ArraySegment<byte>(responseBuffer);
                    await webSocketContext.WebSocket
                        .SendAsync(responseSegment, WebSocketMessageType.Binary, true, responseCancellationToken)
                        .ConfigureAwait(false);

                    // Have a pending read
                    using var source = new CancellationTokenSource(s_oneMinute);
                    WebSocketReceiveResult result = await webSocketContext.WebSocket
                        .ReceiveAsync(arraySegment, source.Token)
                        .ConfigureAwait(false);
                    int bytes = result.Count;
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (WebSocketException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }
        }
    }
}
