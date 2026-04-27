// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientWebSocketTransportTests
    {
        private const string IotHubName = "localhost";
        private const int Port = 12345;

        private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_thirtySeconds = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_fiveMinutes = TimeSpan.FromMinutes(5);
        private static readonly Action<TransportAsyncCallbackArgs> s_onReadOperationComplete = OnReadOperationComplete;
        private static readonly Action<TransportAsyncCallbackArgs> s_onWriteOperationComplete = OnWriteOperationComplete;

        private static HttpListener s_listener;
        private static Task s_serverTask;
        private static byte[] s_byteArray = new byte[10] { 0x5, 0x6, 0x7, 0x8, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF };
        private static volatile bool s_isReadComplete;
        private static ClientWebSocketTransport s_clientWebSocketTransport;

#if NET451
        static LegacyClientWebSocketTransport s_legacyClientWebSocketTransport;
#endif

        [ClassInitialize()]
        public static void AssembyInitialize(TestContext testcontext)
        {
            s_listener = new HttpListener();
            s_listener.Prefixes.Add($"http://+:{Port}{WebSocketConstants.UriSuffix}/");
            s_listener.Start();
            s_serverTask = RunWebSocketServer();
        }

        [ClassCleanup()]
        public static void AssemblyCleanup()
        {
            s_listener.Stop();
            s_clientWebSocketTransport.Dispose();
        }

        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        [Ignore] // TODO #581
        public void ClientWebSocketTransportWriteWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_clientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task ClientWebSocketTransportReadWithoutConnectTest()
        {
            var websocket = new ClientWebSocket();
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            if (s_clientWebSocketTransport.ReadAsync(args))
            {
                while (!s_isReadComplete)
                {
                    Thread.Sleep(s_oneSecond);
                }
            }

            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }

        // The following tests can only be run in Administrator mode
        [TestMethod]
        [Ignore] // TODO #581
        public async Task ReadWriteTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            Uri uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);

            // Test Write API
            var args = new TransportAsyncCallbackArgs();
            args.CompletedCallback = s_onWriteOperationComplete;
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_clientWebSocketTransport.WriteAsync(args);

            // Test Read API
            args.CompletedCallback = s_onReadOperationComplete;
            if (s_clientWebSocketTransport.ReadAsync(args))
            {
                while (!s_isReadComplete)
                {
                    Thread.Sleep(s_oneSecond);
                }
            }

            // Once Read operation is complete, close websocket transport
            // Test Close API
            await s_clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task ReadAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            await s_clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);

            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            args.CompletedCallback = s_onReadOperationComplete;
            s_clientWebSocketTransport.ReadAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task WriteAfterCloseTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            var uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            await s_clientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);

            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            args.CompletedCallback = s_onWriteOperationComplete;
            s_clientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task ReadAfterAbortTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            Uri uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            s_clientWebSocketTransport.Abort();
            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            s_clientWebSocketTransport.ReadAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task WriteAfterAbortTest()
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            Uri uri = new Uri($"ws://{IotHubName}:{Port}{WebSocketConstants.UriSuffix}");
            await websocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            s_clientWebSocketTransport = new ClientWebSocketTransport(websocket, null, null);
            s_clientWebSocketTransport.Abort();
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_clientWebSocketTransport.WriteAsync(args);
        }

#if NET451
        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        [Ignore] // TODO #581
        public void LegacyClientWebSocketTransportWriteWithoutConnectTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            var clientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            clientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(AmqpException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyClientWebSocketTransportReadWithoutConnectTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            var clientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            if (clientWebSocketTransport.ReadAsync(args))
            {
                while (!s_isReadComplete)
                {
                    Thread.Sleep(s_oneSecond);
                }
            }

            await websocket.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyWebSocketReadWriteTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            using var cts = new CancellationTokenSource(s_oneMinute);
            await websocket.ConnectAsync(IotHubName, Port, "ws://", null, cts.Token).ConfigureAwait(false);

            s_legacyClientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);

            // Test Write API
            TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
            args.CompletedCallback = s_onWriteOperationComplete;
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_legacyClientWebSocketTransport.WriteAsync(args);

            // Test Read API
            args.CompletedCallback = s_onReadOperationComplete;
            if (s_legacyClientWebSocketTransport.ReadAsync(args))
            {
                while (!s_isReadComplete)
                {
                    Thread.Sleep(s_oneSecond);
                }
            }

            // Once Read operation is complete, close websocket transport
            s_legacyClientWebSocketTransport.CloseAsync(s_thirtySeconds).Wait(CancellationToken.None);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyWebSocketReadAfterCloseTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            using var cts = new CancellationTokenSource(s_oneMinute);
            await websocket.ConnectAsync(IotHubName, Port, "ws://", null, cts.Token).ConfigureAwait(false);
            s_legacyClientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            await s_legacyClientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);
            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            s_legacyClientWebSocketTransport.ReadAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyWebSocketWriteAfterCloseTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            using var cts = new CancellationTokenSource(s_oneMinute);
            await websocket.ConnectAsync(IotHubName, Port, "ws://", null, cts.Token).ConfigureAwait(false);
            s_legacyClientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            await s_legacyClientWebSocketTransport.CloseAsync(s_thirtySeconds).ConfigureAwait(false);
            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_legacyClientWebSocketTransport.WriteAsync(args);
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyWebSocketReadAfterAbortTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            using var cts = new CancellationTokenSource(s_oneMinute);
            await websocket.ConnectAsync(IotHubName, Port, "ws://", null, cts.Token).ConfigureAwait(false);
            s_legacyClientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            s_legacyClientWebSocketTransport.Abort();

            var args = new TransportAsyncCallbackArgs();
            var byteArray = new byte[10];
            args.SetBuffer(byteArray, 0, 10);
            s_legacyClientWebSocketTransport.ReadAsync(args);
            Assert.Fail("Did not throw object disposed exception");
        }

        [ExpectedException(typeof(ObjectDisposedException))]
        [TestMethod]
        [Ignore] // TODO #581
        public async Task LegacyWebSocketWriteAfterAbortTest()
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            using var cts = new CancellationTokenSource(s_oneMinute);
            await websocket.ConnectAsync(IotHubName, Port, "ws://", null, cts.Token).ConfigureAwait(false);
            s_legacyClientWebSocketTransport = new LegacyClientWebSocketTransport(websocket, s_oneMinute, null, null);
            s_legacyClientWebSocketTransport.Abort();

            var args = new TransportAsyncCallbackArgs();
            args.SetBuffer(s_byteArray, 0, s_byteArray.Length);
            s_legacyClientWebSocketTransport.WriteAsync(args);
            Assert.Fail("Did not throw object disposed exception");
        }
#endif

        private static void OnWriteOperationComplete(TransportAsyncCallbackArgs args)
        {
            if (args.BytesTransfered != s_byteArray.Length)
            {
                throw new InvalidOperationException("All the bytes sent were not transferred");
            }

            if (args.Exception != null)
            {
                throw args.Exception;
            }
        }

        private static void OnReadOperationComplete(TransportAsyncCallbackArgs args)
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

            s_isReadComplete = true;
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

                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(WebSocketConstants.SubProtocols.Amqpwsb10, 8 * 1024, s_fiveMinutes).ConfigureAwait(false);

                    var buffer = new byte[1 * 1024];
                    var arraySegment = new ArraySegment<byte>(buffer);
                    var cancellationToken = new CancellationToken();
                    WebSocketReceiveResult receiveResult = await webSocketContext.WebSocket.ReceiveAsync(arraySegment, cancellationToken).ConfigureAwait(false);

                    // Echo the data back to the client
                    var responseCancellationToken = new CancellationToken();
                    var responseBuffer = new byte[receiveResult.Count];
                    for (int i = 0; i < receiveResult.Count; i++)
                    {
                        responseBuffer[i] = arraySegment.Array[i];
                    }

                    var responseSegment = new ArraySegment<byte>(responseBuffer);
                    await webSocketContext.WebSocket.SendAsync(responseSegment, WebSocketMessageType.Binary, true, responseCancellationToken).ConfigureAwait(false);

                    // Have a pending read
                    using var source = new CancellationTokenSource(s_oneMinute);
                    WebSocketReceiveResult result = await webSocketContext.WebSocket.ReceiveAsync(arraySegment, source.Token).ConfigureAwait(false);
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
