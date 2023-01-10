// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Client
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

            if (args.Buffer == null && args.ByteBufferList == null)
            {
                throw new InvalidOperationException("Must have a buffer to write.");
            }
            if (args.CompletedCallback == null)
            {
                throw new InvalidOperationException("Must have a valid callback.");
            }
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
            if (args.Buffer == null)
            {
                throw new InvalidOperationException("Must have buffer to read.");
            }
            if (args.CompletedCallback == null)
            {
                throw new InvalidOperationException("Must have a valid callback.");
            }

            ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
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
                throw new IotHubClientException(webSocketException.Message, IotHubClientErrorCode.NetworkErrors, webSocketException);
            }
            catch (HttpListenerException httpListenerException)
            {
                throw new IotHubClientException(httpListenerException.Message, IotHubClientErrorCode.NetworkErrors, httpListenerException);
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
                throw new IotHubClientException(webSocketException.Message, IotHubClientErrorCode.NetworkErrors, webSocketException);
            }
            catch (HttpListenerException httpListenerException)
            {
                throw new IotHubClientException(httpListenerException.Message, IotHubClientErrorCode.NetworkErrors, httpListenerException);
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
                // If the task is canceled, this will throw a TaskCanceledException, which we expect to bubble up to the user
                args.BytesTransfered = taskResult.Result;

                args.CompletedSynchronously = ((IAsyncResult)taskResult).CompletedSynchronously;
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

        private static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            Argument.AssertNotNull(buffer, nameof(buffer));

            if (offset < 0 || offset > buffer.Length || size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "The buffer bounds are invalid.");
            }

            int remainingBufferSpace = buffer.Length - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "The buffer bounds are invalid.");
            }
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
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<object>(state, TaskCreationOptions.RunContinuationsAsynchronously);
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

            var tcs = new TaskCompletionSource<TResult>(state, TaskCreationOptions.RunContinuationsAsynchronously);
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
