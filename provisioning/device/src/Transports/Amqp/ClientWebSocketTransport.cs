// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal sealed class ClientWebSocketTransport : TransportBase, IDisposable
    {
        private static readonly AsyncCallback s_onReadComplete = OnReadComplete;
        private static readonly AsyncCallback s_onWriteComplete = OnWriteComplete;
        private static readonly TimeSpan s_closeTimeout = TimeSpan.FromSeconds(30);

        private readonly ClientWebSocket _webSocket;
        private readonly EndPoint _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private CancellationTokenSource _writeCancellationTokenSource;
        private bool _disposed;

        public ClientWebSocketTransport(ClientWebSocket clientwebSocket, EndPoint localEndpoint, EndPoint remoteEndpoint)
            : base("clientwebsocket")
        {
            _webSocket = clientwebSocket;
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

            args.Exception = null; // null out any exceptions

            Task<bool> taskResult = WriteAsyncCoreAsync(args);
            if (WriteTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(s_onWriteComplete, args);
            return true;
        }

        private async Task<bool> WriteAsyncCoreAsync(TransportAsyncCallbackArgs args)
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
                        await _webSocket.SendAsync(new ArraySegment<byte>(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length),
                            WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }

                succeeded = true;
            }
            catch (WebSocketException webSocketException)
            {
                throw new DeviceProvisioningClientException(webSocketException.Message, webSocketException, true);
            }
            catch (HttpListenerException httpListenerException)
            {
                throw new DeviceProvisioningClientException(httpListenerException.Message, httpListenerException, true);
            }
            finally
            {
                if (!succeeded)
                {
                    Abort();
                }
            }

            return succeeded;
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            ThrowIfNotOpen();

            // Read with buffer list not supported
            Debug.Assert(args.Buffer != null, "must have buffer to read");
            Debug.Assert(args.CompletedCallback != null, "must have a valid callback");

            var arraySegment = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);

            args.Exception = null;   // null out any exceptions
            Task<int> taskResult = ReadCoreAsync(arraySegment);
            if (ReadTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(s_onReadComplete, args);
            return true;
        }

        private async Task<int> ReadCoreAsync(ArraySegment<byte> seg)
        {
            bool succeeded = false;
            try
            {
                WebSocketReceiveResult receiveResult = await _webSocket
                    .ReceiveAsync(seg, CancellationToken.None)
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
            WebSocketState webSocketState = _webSocket.State;
            if (webSocketState != WebSocketState.Closed && webSocketState != WebSocketState.Aborted)
            {
                Task.Run(() => CloseInternalAsync(s_closeTimeout));
            }

            return true;
        }

        private async Task CloseInternalAsync(TimeSpan timeout)
        {
            try
            {
                // Cancel any pending write
                CancelPendingWrite();

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception)
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
            if (!_disposed)
            {
                if (_webSocket.State != WebSocketState.Aborted)
                {
                    _webSocket.Abort();
                    _webSocket.Dispose();
                }

                _disposed = true;
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
            IAsyncResult result = taskResult;
            args.BytesTransfered = 0; // reset bytes transferred
            if (taskResult.IsFaulted)
            {
                args.CompletedSynchronously = result.CompletedSynchronously;
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
                args.CompletedSynchronously = result.CompletedSynchronously;
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
            _writeCancellationTokenSource?.Dispose();
            _writeCancellationTokenSource = null;
        }
    }
}
