// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Amqp.Transport
{
    internal sealed class ClientWebSocketTransport : TransportBase, IDisposable
    {
        private static readonly AsyncCallback s_onReadComplete = OnReadComplete;
        private static readonly AsyncCallback s_onWriteComplete = OnWriteComplete;
        private static readonly TimeSpan s_closeTimeout = TimeSpan.FromSeconds(30);

        private static readonly WebSocketState[] s_closedWebsocketStates = new[]
        {
            WebSocketState.Aborted,
            WebSocketState.Closed,
            WebSocketState.CloseReceived,
            WebSocketState.CloseSent,
        };

        private readonly ClientWebSocket _webSocket;
        private readonly EndPoint _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private readonly CancellationTokenSource _writeCancellationTokenSource;
        private bool _isDisposed;

        public ClientWebSocketTransport(ClientWebSocket webSocket, EndPoint localEndpoint, EndPoint remoteEndpoint)
            : base("clientwebsocket")
        {
            _webSocket = webSocket;
            _localEndPoint = localEndpoint;
            _remoteEndPoint = remoteEndpoint;
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        public override string LocalEndPoint => _localEndPoint.ToString();

        public override string RemoteEndPoint => _remoteEndPoint.ToString();

        public override bool RequiresCompleteFrames => true;

        public override bool IsSecure => true;

        public override void SetMonitor(ITransportMonitor usageMeter)
        {
            // Do nothing
        }

        public override bool WriteAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            Fx.AssertAndThrow(args.Buffer != null || args.ByteBufferList != null, "must have a buffer to write");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");
            args.Exception = null; // null out any exceptions

            Task taskResult = WriteImplAsync(args);
            if (WriteTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(s_onWriteComplete, args);
            return true;
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            // Read with buffer list not supported
            Fx.AssertAndThrow(args.Buffer != null, "must have buffer to read");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");

            Utils.ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
            args.Exception = null; // null out any exceptions

            Task<int> taskResult = ReadImplAsync(args);
            if (ReadTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(s_onReadComplete, args);
            return true;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _webSocket.Abort();
                _webSocket.Dispose();

                _writeCancellationTokenSource.Dispose();

                _isDisposed = true;
            }
        }

        protected override bool OpenInternal()
        {
            ThrowIfNotOpen();

            return true;
        }

        protected override bool CloseInternal()
        {
            WebSocketState webSocketState = _webSocket.State;
            if (webSocketState != WebSocketState.Closed
                && webSocketState != WebSocketState.Aborted)
            {
                CloseImplAsync(s_closeTimeout).GetAwaiter().GetResult();
            }

            return true;
        }

        protected override void AbortInternal()
        {
            Dispose();
        }

        private async Task<int> ReadImplAsync(TransportAsyncCallbackArgs args)
        {
            bool succeeded = false;
            try
            {
                WebSocketReceiveResult receiveResult = await _webSocket
                    .ReceiveAsync(new ArraySegment<byte>(args.Buffer, args.Offset, args.Count), CancellationToken.None)
                    .ConfigureAwait(false);

                succeeded = true;
                return receiveResult.Count;
            }
            catch (WebSocketException webSocketException)
            {
                throw new IOException(webSocketException.Message, webSocketException);
            }
            catch (HttpListenerException httpListenerException)
            {
                throw new IOException(httpListenerException.Message, httpListenerException);
            }
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

        private async Task WriteImplAsync(TransportAsyncCallbackArgs args)
        {
            bool succeeded = false;
            try
            {
                if (args.Buffer != null)
                {
                    var arraySegment = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);
                    await _webSocket
                        .SendAsync(arraySegment, WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }
                else
                {
                    foreach (ByteBuffer byteBuffer in args.ByteBufferList)
                    {
                        await _webSocket
                            .SendAsync(
                                new ArraySegment<byte>(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length),
                                WebSocketMessageType.Binary,
                                true,
                                _writeCancellationTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                }

                succeeded = true;
            }
            catch (WebSocketException webSocketException)
            {
                throw new IOException(webSocketException.Message, webSocketException);
            }
            catch (HttpListenerException httpListenerException)
            {
                throw new IOException(httpListenerException.Message, httpListenerException);
            }
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

        private async Task CloseImplAsync(TimeSpan timeout)
        {
            try
            {
                // Cancel any pending write
                CancelPendingWrite();

                using var cancellationTokenSource = new CancellationTokenSource(timeout);
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
            }

            // Call Abort anyway to ensure that all WebSocket Resources are released
            Abort();
        }

        private void CancelPendingWrite()
        {
            try
            {
                _writeCancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore this error
            }
        }

        private static void OnReadComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            HandleReadComplete(result);
        }

        private static void HandleReadComplete(IAsyncResult result)
        {
            var taskResult = (Task<int>)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;

            ReadTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        private static bool ReadTaskDone(Task<int> taskResult, TransportAsyncCallbackArgs args)
        {
            args.BytesTransfered = 0; // reset bytes transferred

            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }
            else if (taskResult.IsCompleted)
            {
                args.BytesTransfered = taskResult.Result;
                args.CompletedSynchronously = ((IAsyncResult)taskResult).CompletedSynchronously;
                return true;
            }
            else if (taskResult.IsCanceled) // This should not happen since TaskCanceledException is handled in ReadAsyncCore.
            {
                return true;
            }

            return false;
        }

        private static void OnWriteComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            HandleWriteComplete(result);
        }

        private static void HandleWriteComplete(IAsyncResult result)
        {
            var taskResult = (Task)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;
            WriteTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        private static bool WriteTaskDone(Task taskResult, TransportAsyncCallbackArgs args)
        {
            args.BytesTransfered = 0; // reset bytes transferred

            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }
            else if (taskResult.IsCompleted)
            {
                args.BytesTransfered = args.Count;
                args.CompletedSynchronously = ((IAsyncResult)taskResult).CompletedSynchronously;
                return true;
            }
            else if (taskResult.IsCanceled) // This should not happen since TaskCanceledException is handled in WriteAsyncCore.
            {
                return true;
            }

            return false;
        }

        private void ThrowIfNotOpen()
        {
            WebSocketState webSocketState = _webSocket.State;
            if (webSocketState == WebSocketState.Open)
            {
                return;
            }

            if (s_closedWebsocketStates.Contains(webSocketState))
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            throw new AmqpException(AmqpErrorCode.IllegalState, null);
        }
    }
}
