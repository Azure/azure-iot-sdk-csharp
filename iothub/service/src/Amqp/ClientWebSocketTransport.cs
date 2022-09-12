// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Our web socket implementation based off of Azure.AMQP transport.
    /// </summary>
    internal sealed class ClientWebSocketTransport : TransportBase, IDisposable
    {
        private static readonly AsyncCallback s_onReadComplete = OnReadComplete;
        private static readonly AsyncCallback s_onWriteComplete = OnWriteComplete;
        private static readonly TimeSpan s_closeTimeout = TimeSpan.FromSeconds(30);

        private readonly EndPoint _localEndPoint;
        private readonly EndPoint _remoteEndPoint;

        // Disposables

        private readonly ClientWebSocket _webSocket;
        private readonly CancellationTokenSource _writeCancellationTokenSource;
        private bool _isDisposed;

        internal ClientWebSocketTransport(ClientWebSocket webSocket, EndPoint localEndpoint, EndPoint remoteEndpoint)
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
            // Do Nothing
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

            ToAsyncResult(taskResult, s_onWriteComplete, args);
            return true;
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            // Read with buffer list not supported
            Fx.AssertAndThrow(args.Buffer != null, "must have buffer to read");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");

            Argument.ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
            args.Exception = null; // null out any exceptions

            Task<int> taskResult = ReadImplAsync(args);
            if (ReadTaskDone(taskResult, args))
            {
                return false;
            }

            ToAsyncResult(taskResult, s_onReadComplete, args);
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
            if (_webSocket.State != WebSocketState.Closed && _webSocket.State != WebSocketState.Aborted)
            {
                // Do not wait
                _ = CloseImplAsync(s_closeTimeout);
            }

            return true;
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

        protected override void AbortInternal()
        {
            Dispose();
        }

        private async Task WriteImplAsync(TransportAsyncCallbackArgs args)
        {
            bool succeeded = false;
            try
            {
                if (args.Buffer != null)
                {
                    var arraySegment = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);
                    await _webSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    foreach (ByteBuffer byteBuffer in args.ByteBufferList)
                    {
                        await _webSocket
                            .SendAsync(
                                new ArraySegment<byte>(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length),
                                WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token)
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
            IAsyncResult result = taskResult;
            args.BytesTransfered = 0; // reset bytes transferred

            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }

            if (taskResult.IsCompleted)
            {
                args.BytesTransfered = taskResult.Result;
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }

            return taskResult.IsCanceled;
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

            if (taskResult.IsCompleted)
            {
                args.BytesTransfered = args.Count;
                args.CompletedSynchronously = ((IAsyncResult)taskResult).CompletedSynchronously;
                return true;
            }

            return taskResult.IsCanceled;
        }

        private void ThrowIfNotOpen()
        {
            WebSocketState webSocketState = _webSocket.State;
            if (webSocketState == WebSocketState.Open)
            {
                return;
            }

            if (webSocketState == WebSocketState.Aborted
                || webSocketState == WebSocketState.Closed
                || webSocketState == WebSocketState.CloseReceived
                || webSocketState == WebSocketState.CloseSent)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            throw new AmqpException(AmqpErrorCode.IllegalState, null);
        }

        private static IAsyncResult ToAsyncResult(Task task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }

                    callback?.Invoke(tcs.Task);
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        private static IAsyncResult ToAsyncResult<TResult>(Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        CancellationToken.None,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(task.Result);
                    }

                    callback?.Invoke(tcs.Task);
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }
    }
}
