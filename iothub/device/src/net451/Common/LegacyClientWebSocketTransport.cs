// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class LegacyClientWebSocketTransport : TransportBase
    {
        private const string ClientWebSocketTransportReadBufferTooSmall = "LegacyClientWebSocketTransport Read Buffer too small.";
        private const int MaxReadBufferSize = 256 * 1024; // Max Read buffer size is hard-coded to 256k

        private static readonly AsyncCallback s_onWriteComplete = OnWriteComplete;

        private readonly IotHubClientWebSocket _webSocket;
        private readonly EndPoint _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private readonly TimeSpan _operationTimeout;
        private readonly int _asyncReadBufferSize;
        private readonly byte[] _asyncReadBuffer;

        private bool _isDisposed;
        private int _asyncReadBufferOffset;
        private int _remainingBytes;

        public LegacyClientWebSocketTransport(
            IotHubClientWebSocket webSocket,
            TimeSpan operationTimeout,
            EndPoint localEndpoint,
            EndPoint remoteEndpoint)
            : base("legacyclientwebsocket")
        {
            _webSocket = webSocket;
            _operationTimeout = operationTimeout;
            _localEndPoint = localEndpoint;
            _remoteEndPoint = remoteEndpoint;
            _asyncReadBufferSize = MaxReadBufferSize; // TODO: read from Config Settings
            _asyncReadBuffer = new byte[_asyncReadBufferSize];
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
            if (Logging.IsEnabled)
                Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsync)}");

            ThrowIfNotOpen();

            Fx.AssertAndThrow(args.Buffer != null || args.ByteBufferList != null, "must have a buffer to write");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");
            args.Exception = null; // null out any exceptions

            Task taskResult = WriteCoreAsync(args);
            if (WriteTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(s_onWriteComplete, args);

            if (Logging.IsEnabled)
                Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsync)}");

            return true;
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsync)}");

            ThrowIfNotOpen();

            // Read with buffer list not supported
            Fx.AssertAndThrow(args.Buffer != null, "must have buffer to read");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");

            // TODO: Is this assert valid at all?  It should be ok for caller to ask for more bytes than we can give...
            Fx.AssertAndThrow(args.Count <= _asyncReadBufferSize, ClientWebSocketTransportReadBufferTooSmall);

            Utils.ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
            args.Exception = null;

            if (_asyncReadBufferOffset > 0)
            {
                Fx.AssertAndThrow(_remainingBytes > 0, "Must have data in buffer to transfer");

                // Data left over from previous read
                TransferData(_remainingBytes, args);
                return false;
            }

            args.Exception = null; // null out any exceptions
            Task<int> taskResult = ReadCoreAsync();

            if (ReadTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(OnReadComplete, args);

            if (Logging.IsEnabled)
                Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsync)}");

            return true;
        }

        private async Task WriteCoreAsync(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteCoreAsync)}");

            bool succeeded = false;
            try
            {
                if (args.Buffer != null)
                {
                    await _webSocket
                        .SendAsync(args.Buffer, args.Offset, args.Count, IotHubClientWebSocket.WebSocketMessageType.Binary, _operationTimeout)
                        .ConfigureAwait(false);
                }
                else
                {
                    foreach (ByteBuffer byteBuffer in args.ByteBufferList)
                    {
                        await _webSocket
                            .SendAsync(
                                byteBuffer.Buffer,
                                byteBuffer.Offset,
                                byteBuffer.Length,
                                IotHubClientWebSocket.WebSocketMessageType.Binary,
                                _operationTimeout)
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
                if (Logging.IsEnabled)
                    Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteCoreAsync)}");
            }
        }

        protected override bool OpenInternal()
        {
            ThrowIfNotOpen();

            return true;
        }

        protected override bool CloseInternal()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternal)}");

                IotHubClientWebSocket.WebSocketState webSocketState = _webSocket.State;
                if (webSocketState != IotHubClientWebSocket.WebSocketState.Closed
                    && webSocketState != IotHubClientWebSocket.WebSocketState.Aborted)
                {
                    CloseInternalAsync().GetAwaiter().GetResult();
                }
                return true;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternal)}");
            }
        }

        protected override void AbortInternal()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(AbortInternal)}");

                if (!_isDisposed
                    && _webSocket.State != IotHubClientWebSocket.WebSocketState.Aborted)
                {
                    _isDisposed = true;
                    _webSocket.Abort();
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(AbortInternal)}");
            }
        }

        private async Task<int> ReadCoreAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadCoreAsync)}");

            bool succeeded = false;
            try
            {
                int numBytes = await _webSocket.ReceiveAsync(_asyncReadBuffer, _asyncReadBufferOffset, _operationTimeout).ConfigureAwait(false);

                succeeded = true;
                return numBytes;
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
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadCoreAsync)}");
                }
            }
        }

        private async Task CloseInternalAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternalAsync)}");

                await _webSocket.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (!Fx.IsFatal(e))
            {
                // do nothing
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternalAsync)}");
            }
        }

        private void OnReadComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            var taskResult = (Task<int>)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;

            ReadTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        private bool ReadTaskDone(Task<int> taskResult, TransportAsyncCallbackArgs args)
        {
            IAsyncResult result = taskResult;
            args.BytesTransfered = 0;  // reset bytes transferred
            if (taskResult.IsFaulted)
            {
                args.Exception = taskResult.Exception;
                return true;
            }

            if (taskResult.IsCompleted)
            {
                TransferData(taskResult.Result, args);
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }

            // This should not be canceled since TaskCanceledException is handled in ReadCoreAsync.
            return taskResult.IsCanceled;
        }

        private void TransferData(int bytesRead, TransportAsyncCallbackArgs args)
        {
            if (bytesRead <= args.Count)
            {
                Buffer.BlockCopy(_asyncReadBuffer, _asyncReadBufferOffset, args.Buffer, args.Offset, bytesRead);
                _asyncReadBufferOffset = 0;
                _remainingBytes = 0;
                args.BytesTransfered = bytesRead;
            }
            else
            {
                Buffer.BlockCopy(_asyncReadBuffer, _asyncReadBufferOffset, args.Buffer, args.Offset, args.Count);

                // read only part of the data
                _asyncReadBufferOffset += args.Count;
                _remainingBytes = bytesRead - args.Count;
                args.BytesTransfered = args.Count;
            }
        }

        private static void OnWriteComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

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
                args.Exception = taskResult.Exception;
                return true;
            }

            if (taskResult.IsCompleted)
            {
                args.BytesTransfered = args.Count;
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }

            // This should not be true since TaskCanceledException is handled in WriteCoreAsync.
            return taskResult.IsCanceled;
        }

        private void ThrowIfNotOpen()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ThrowIfNotOpen)}");

                IotHubClientWebSocket.WebSocketState webSocketState = _webSocket.State;
                if (webSocketState == IotHubClientWebSocket.WebSocketState.Open)
                {
                    return;
                }

                if (webSocketState == IotHubClientWebSocket.WebSocketState.Aborted
                    || webSocketState == IotHubClientWebSocket.WebSocketState.Closed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                throw new AmqpException(AmqpErrorCode.IllegalState, null);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ThrowIfNotOpen)}");
            }
        }
    }
}
