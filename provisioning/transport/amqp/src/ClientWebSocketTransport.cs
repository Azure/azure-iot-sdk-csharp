// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Amqp.Transport
{
    internal sealed class ClientWebSocketTransport : TransportBase, IDisposable
    {
        static readonly AsyncCallback onReadComplete = OnReadComplete;
        static readonly AsyncCallback onWriteComplete = OnWriteComplete;
        static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(30);

        readonly ClientWebSocket webSocket;
        readonly EndPoint localEndPoint;
        readonly EndPoint remoteEndPoint;
        volatile CancellationTokenSource writeCancellationTokenSource;
        bool disposed;

        public ClientWebSocketTransport(ClientWebSocket clientwebSocket, EndPoint localEndpoint, EndPoint remoteEndpoint)
            : base("clientwebsocket")
        {
            webSocket = clientwebSocket;
            localEndPoint = localEndpoint;
            remoteEndPoint = remoteEndpoint;
            writeCancellationTokenSource = new CancellationTokenSource();
        }

        public override EndPoint LocalEndPoint
        {
            get { return localEndPoint; }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return remoteEndPoint; }
        }

        public override bool RequiresCompleteFrames
        {
            get { return true; }
        }

        public override bool IsSecure
        {
            get { return true; }
        }

        public override void SetMonitor(ITransportMonitor usageMeter)
        {
            // Do Nothing
        }

        public override bool WriteAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            args.Exception = null; // null out any exceptions

            Task taskResult = WriteAsyncCore(args);
            if (WriteTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(onWriteComplete, args);
            return true;
        }

        async Task WriteAsyncCore(TransportAsyncCallbackArgs args)
        {
            bool succeeded = false;
            try
            {
                if (args.Buffer != null)
                {
                    var arraySegment = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, writeCancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    foreach (ByteBuffer byteBuffer in args.ByteBufferList)
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length),
                            WebSocketMessageType.Binary, true, writeCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }

                succeeded = true;
            }
            catch (WebSocketException webSocketException)
            {
                throw new IOException(webSocketException.Message, webSocketException);
            }
#if !NETSTANDARD1_3
            catch (HttpListenerException httpListenerException)
            {
                throw new IOException(httpListenerException.Message, httpListenerException);
            }
#endif
            catch (TaskCanceledException taskCanceledException)
            {
                throw new TimeoutException(taskCanceledException.Message, taskCanceledException);
            }
            finally
            {
                if (!succeeded)
                {
                    Abort();
                }
            }
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            // Read with buffer list not supported
            Debug.Assert(args.Buffer != null, "must have buffer to read");
            Debug.Assert(args.CompletedCallback != null, "must have a valid callback");

            var arraySegment = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);

            args.Exception = null;   // null out any exceptions
            Task<int> taskResult = ReadAsyncCore(arraySegment);
            if (ReadTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(onReadComplete, args);
            return true;
        }

        async Task<int> ReadAsyncCore(ArraySegment<byte> seg)
        {
            bool succeeded = false;
            try
            {
                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                    seg, CancellationToken.None).ConfigureAwait(false);

                succeeded = true;
                return receiveResult.Count;
            }
            catch (WebSocketException webSocketException)
            {
                throw new IOException(webSocketException.Message, webSocketException);
            }
#if !NETSTANDARD1_3
            catch (HttpListenerException httpListenerException)
            {
                throw new IOException(httpListenerException.Message, httpListenerException);
            }
#endif
            catch (TaskCanceledException taskCanceledException)
            {
                throw new TimeoutException(taskCanceledException.Message, taskCanceledException);
            }
            finally
            {
                if (!succeeded)
                {
                    Abort();
                }
            }
        }

        protected override bool OpenInternal()
        {
            ThrowIfNotOpen();

            return true;
        }

        protected override bool CloseInternal()
        {
            var webSocketState = webSocket.State;
            if (webSocketState != WebSocketState.Closed && webSocketState != WebSocketState.Aborted)
            {
                Task.Run(() => CloseInternalAsync(CloseTimeout));
            }

            return true;
        }

        async Task CloseInternalAsync(TimeSpan timeout)
        {
            try
            {
                // Cancel any pending write
                CancelPendingWrite();

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }

            // Call Abort anyway to ensure that all WebSocket Resources are released 
            Abort();
        }

        void CancelPendingWrite()
        {
            try
            {
                writeCancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore this error
            }
        }

        protected override void AbortInternal()
        {
            if (!disposed && webSocket.State != WebSocketState.Aborted)
            {
                disposed = true;
                webSocket.Abort();
                webSocket.Dispose();
            }
        }

        static void OnReadComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            HandleReadComplete(result);
        }

        static void HandleReadComplete(IAsyncResult result)
        {
            Task<int> taskResult = (Task<int>)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;

            ReadTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        static bool ReadTaskDone(Task<int> taskResult, TransportAsyncCallbackArgs args)
        {
            IAsyncResult result = taskResult;
            args.BytesTransfered = 0;  // reset bytes transferred
            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }
            else if (taskResult.IsCompleted)
            {
                args.BytesTransfered = taskResult.Result;
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }
            else if (taskResult.IsCanceled)  // This should not happen since TaskCanceledException is handled in ReadAsyncCore.
            {
                return true;
            }

            return false;
        }

        static void OnWriteComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            HandleWriteComplete(result);
        }

        static void HandleWriteComplete(IAsyncResult result)
        {
            Task taskResult = (Task)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;
            WriteTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        static bool WriteTaskDone(Task taskResult, TransportAsyncCallbackArgs args)
        {
            IAsyncResult result = taskResult;
            args.BytesTransfered = 0; // reset bytes transferred
            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }
            else if (taskResult.IsCompleted)
            {
                args.BytesTransfered = args.Count;
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }
            else if (taskResult.IsCanceled)  // This should not happen since TaskCanceledException is handled in WriteAsyncCore.
            {
                return true;
            }

            return false;
        }

        void ThrowIfNotOpen()
        {
            var webSocketState = webSocket.State;
            if (webSocketState == WebSocketState.Open)
            {
                return;
            }

            if (webSocketState == WebSocketState.Aborted ||
                webSocketState == WebSocketState.Closed ||
                webSocketState == WebSocketState.CloseReceived ||
                webSocketState == WebSocketState.CloseSent)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            throw new AmqpException(AmqpErrorCode.IllegalState, null);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
