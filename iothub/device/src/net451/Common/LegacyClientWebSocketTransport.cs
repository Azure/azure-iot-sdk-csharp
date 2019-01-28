// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Transport;
    using Microsoft.Azure.Devices.Shared;

    sealed class LegacyClientWebSocketTransport : TransportBase
    {
        const string ClientWebSocketTransportReadBufferTooSmall = "LegacyClientWebSocketTransport Read Buffer too small.";
        const int MaxReadBufferSize = 256 * 1024; // Max Read buffer size is hard-coded to 256k
        static readonly AsyncCallback onWriteComplete = OnWriteComplete;

        readonly IotHubClientWebSocket webSocket;
        readonly EndPoint localEndPoint;
        readonly EndPoint remoteEndPoint;
        readonly TimeSpan operationTimeout;
        readonly int asyncReadBufferSize;
        readonly byte[] asyncReadBuffer;

        bool disposed;
        int asyncReadBufferOffset;
        int remainingBytes;

        public LegacyClientWebSocketTransport(IotHubClientWebSocket webSocket, TimeSpan operationTimeout, EndPoint localEndpoint, EndPoint remoteEndpoint)
            : base("legacyclientwebsocket")
        {
            this.webSocket = webSocket;
            this.operationTimeout = operationTimeout;
            this.localEndPoint = localEndpoint;
            this.remoteEndPoint = remoteEndpoint;
            this.asyncReadBufferSize = MaxReadBufferSize; // TODO: read from Config Settings
            this.asyncReadBuffer = new byte[this.asyncReadBufferSize];
        }

        public override string LocalEndPoint
        {
            get { return this.localEndPoint.ToString(); }
        }

        public override string RemoteEndPoint
        {
            get { return this.remoteEndPoint.ToString(); }
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
            if (Logging.IsEnabled) Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsync)}");

            this.ThrowIfNotOpen();

            Fx.AssertAndThrow(args.Buffer != null || args.ByteBufferList != null, "must have a buffer to write");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");
            args.Exception = null; // null out any exceptions

            Task taskResult = this.WriteAsyncCore(args);
            if (WriteTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(onWriteComplete, args);

            if (Logging.IsEnabled) Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsync)}");
            return true;
        }

        async Task WriteAsyncCore(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsyncCore)}");

            bool succeeded = false;
            try
            {
                if (args.Buffer != null)
                {
                    await this.webSocket.SendAsync(args.Buffer, args.Offset, args.Count, IotHubClientWebSocket.WebSocketMessageType.Binary, this.operationTimeout).ConfigureAwait(false);
                }
                else
                {
                    foreach (ByteBuffer byteBuffer in args.ByteBufferList)
                    {
                        await this.webSocket.SendAsync(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length, IotHubClientWebSocket.WebSocketMessageType.Binary, this.operationTimeout).ConfigureAwait(false);
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
                    this.Abort();
                }
                if (Logging.IsEnabled) Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(WriteAsyncCore)}");
            }
        }

        async Task<int> ReadAsyncCore()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsyncCore)}");

            bool succeeded = false;
            try
            {
                int numBytes = await this.webSocket.ReceiveAsync(this.asyncReadBuffer, this.asyncReadBufferOffset, this.asyncReadBufferSize, this.operationTimeout).ConfigureAwait(false);

                succeeded = true;
                return numBytes;
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
                    this.Abort();
                }
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsyncCore)}");
            }
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsync)}");

            this.ThrowIfNotOpen();

            // Read with buffer list not supported
            Fx.AssertAndThrow(args.Buffer != null, "must have buffer to read");
            Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");

            // TODO: Is this assert valid at all?  It should be ok for caller to ask for more bytes than we can give...
            Fx.AssertAndThrow(args.Count <= this.asyncReadBufferSize, ClientWebSocketTransportReadBufferTooSmall);

            Utils.ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
            args.Exception = null;

            if (this.asyncReadBufferOffset > 0)
            {
                Fx.AssertAndThrow(this.remainingBytes > 0, "Must have data in buffer to transfer");

                // Data left over from previous read
                this.TransferData(this.remainingBytes, args);
                return false;
            }

            args.Exception = null; // null out any exceptions
            Task<int> taskResult = this.ReadAsyncCore();

            if (this.ReadTaskDone(taskResult, args))
            {
                return false;
            }

            taskResult.ToAsyncResult(this.OnReadComplete, args);

            if (Logging.IsEnabled) Logging.Exit(this, args.Transport, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ReadAsync)}");
            return true;
        }

        protected override bool OpenInternal()
        {
            this.ThrowIfNotOpen();

            return true;
        }

        protected override bool CloseInternal()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternal)}");

                var webSocketState = this.webSocket.State;
                if (webSocketState != IotHubClientWebSocket.WebSocketState.Closed && webSocketState != IotHubClientWebSocket.WebSocketState.Aborted)
                {
                    this.CloseInternalAsync();
                }
                return true;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternal)}");
            }            
        }

        async Task CloseInternalAsync()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternalAsync)}");

                await this.webSocket.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(CloseInternalAsync)}");
            }
        }

        protected override void AbortInternal()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(AbortInternal)}");

                if (!this.disposed && this.webSocket.State != IotHubClientWebSocket.WebSocketState.Aborted)
                {
                    this.disposed = true;
                    this.webSocket.Abort();
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(AbortInternal)}");
            }
        }

        void OnReadComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            Task<int> taskResult = (Task<int>)result;
            var args = (TransportAsyncCallbackArgs)taskResult.AsyncState;

            this.ReadTaskDone(taskResult, args);
            args.CompletedCallback(args);
        }

        bool ReadTaskDone(Task<int> taskResult, TransportAsyncCallbackArgs args)
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
                this.TransferData(taskResult.Result, args);
                args.CompletedSynchronously = result.CompletedSynchronously;
                return true;
            }
            else if (taskResult.IsCanceled)  // This should not happen since TaskCanceledException is handled in ReadAsyncCore.
            {
                return true;
            }

            return false;
        }

        void TransferData(int bytesRead, TransportAsyncCallbackArgs args)
        {
            if (bytesRead <= args.Count)
            {
                Buffer.BlockCopy(this.asyncReadBuffer, this.asyncReadBufferOffset, args.Buffer, args.Offset, bytesRead);
                this.asyncReadBufferOffset = 0;
                this.remainingBytes = 0;
                args.BytesTransfered = bytesRead;
            }
            else
            {
                Buffer.BlockCopy(this.asyncReadBuffer, this.asyncReadBufferOffset, args.Buffer, args.Offset, args.Count);

                // read only part of the data
                this.asyncReadBufferOffset += args.Count;
                this.remainingBytes = bytesRead - args.Count;
                args.BytesTransfered = args.Count;
            }
        }

        static void OnWriteComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

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
           try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ThrowIfNotOpen)}");

                var webSocketState = this.webSocket.State;
                if (webSocketState == IotHubClientWebSocket.WebSocketState.Open)
                {
                    return;
                }

                if (webSocketState == IotHubClientWebSocket.WebSocketState.Aborted ||
                    webSocketState == IotHubClientWebSocket.WebSocketState.Closed
                    )
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                throw new AmqpException(AmqpErrorCode.IllegalState, null);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(LegacyClientWebSocketTransport)}.{nameof(ThrowIfNotOpen)}");
            }
        }
    }
}
