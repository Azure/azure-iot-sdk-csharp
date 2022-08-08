// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Test
{
    public class ServerWebSocketChannel : AbstractChannel
    {
        private readonly WebSocket _webSocket;
        private readonly CancellationTokenSource _writeCancellationTokenSource;
        private bool _active;

        public ServerWebSocketChannel(IChannel parent, WebSocket webSocket, EndPoint remoteEndPoint)
            : base(parent)
        {
            _webSocket = webSocket;
            RemoteAddressInternal = remoteEndPoint;
            _active = true;
            Metadata = new ChannelMetadata(false, 16);
            Configuration = new ServerWebSocketChannelConfig();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        internal bool ReadPending { get; set; }

        internal bool WriteInProgress { get; set; }

        public override IChannelConfiguration Configuration { get; }

        public override bool Open => _webSocket.State == WebSocketState.Open;

        public override bool Active => _active;

        public override ChannelMetadata Metadata { get; }

        public ServerWebSocketChannel Option<T>(ChannelOption<T> option, T value)
        {
            Contract.Requires(option != null);

            Configuration.SetOption(option, value);
            return this;
        }

        protected override EndPoint LocalAddressInternal { get; }

        protected override EndPoint RemoteAddressInternal { get; }

        protected override IChannelUnsafe NewUnsafe() => new WebSocketChannelUnsafe(this);

        protected class WebSocketChannelUnsafe : AbstractUnsafe
        {
            public WebSocketChannelUnsafe(AbstractChannel channel)
                :base(channel)
            {
            }

            public override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
            {
                throw new NotSupportedException("ServerWebSocketChannel does not support BindAsync()");
            }

            protected override void Flush0()
            {
                // Flush immediately only when there's no pending flush.
                // If there's a pending flush operation, event loop will call FinishWrite() later,
                // and thus there's no need to call it now.
                if (((ServerWebSocketChannel)channel).WriteInProgress)
                {
                    return;
                }

                base.Flush0();
            }
        }

        protected override bool IsCompatible(IEventLoop eventLoop) => true;

        protected override void DoBind(EndPoint localAddress)
        {
            throw new NotSupportedException("ServerWebSocketChannel does not support DoBind()");
        }

        protected override void DoDisconnect()
        {
            throw new NotSupportedException("ServerWebSocketChannel does not support DoDisconnect()");
        }

        protected override async void DoClose()
        {
            try
            {
                WebSocketState webSocketState = _webSocket.State;
                if (webSocketState != WebSocketState.Closed && webSocketState != WebSocketState.Aborted)
                {
                    // Cancel any pending write
                    CancelPendingWrite();
                    _active = false;

                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)// when (!e.IsFatal())
            {
                Abort();
            }
        }

        protected override async void DoBeginRead()
        {
            IByteBuffer byteBuffer = null;
            IRecvByteBufAllocatorHandle allocHandle = null;
            bool close = false;
            try
            {
                if (!Open || ReadPending)
                {
                    return;
                }

                ReadPending = true;
                IByteBufferAllocator allocator = Configuration.Allocator;
                allocHandle = Configuration.RecvByteBufAllocator.NewHandle();
                allocHandle.Reset(Configuration);
                do
                {
                    byteBuffer = allocHandle.Allocate(allocator);
                    allocHandle.LastBytesRead = await DoReadBytes(byteBuffer).ConfigureAwait(false);
                    if (allocHandle.LastBytesRead <= 0)
                    {
                        // nothing was read -> release the buffer.
                        byteBuffer.Release();
                        byteBuffer = null;
                        close = allocHandle.LastBytesRead < 0;
                        break;
                    }

                    Pipeline.FireChannelRead(byteBuffer);
                    allocHandle.IncMessagesRead(1);
                }
                while (allocHandle.ContinueReading());

                allocHandle.ReadComplete();
                ReadPending = false;
                Pipeline.FireChannelReadComplete();
            }
            catch (Exception e) //when (!e.IsFatal())
            {
                // Since this method returns void, all exceptions must be handled here.
                byteBuffer?.Release();
                allocHandle?.ReadComplete();
                ReadPending = false;
                Pipeline.FireChannelReadComplete();
                Pipeline.FireExceptionCaught(e);
                close = true;
            }

            if (close)
            {
                if (Active)
                {
                    await HandleCloseAsync().ConfigureAwait(false);
                }
            }
        }

        protected override async void DoWrite(ChannelOutboundBuffer channelOutboundBuffer)
        {
            try
            {
                WriteInProgress = true;
                while (true)
                {
                    object currentMessage = channelOutboundBuffer.Current;
                    if (currentMessage == null)
                    {
                        // Wrote all messages.
                        break;
                    }

                    var byteBuffer = currentMessage as IByteBuffer;
                    Fx.AssertAndThrow(byteBuffer != null, "channelOutBoundBuffer contents must be of type IByteBuffer");

                    if (byteBuffer.ReadableBytes == 0)
                    {
                        channelOutboundBuffer.Remove();
                        continue;
                    }

                    await _webSocket.SendAsync(byteBuffer.GetIoBuffer(), WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token).ConfigureAwait(false);
                    channelOutboundBuffer.Remove();
                }

                WriteInProgress = false;
            }
            catch (Exception e) //when (!e.IsFatal())
            {
                // Since this method returns void, all exceptions must be handled here.

                WriteInProgress = false;
                Pipeline.FireExceptionCaught(e);
                await HandleCloseAsync().ConfigureAwait(false);
            }
        }

        async Task<int> DoReadBytes(IByteBuffer byteBuffer)
        {
            WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(byteBuffer.Array, byteBuffer.ArrayOffset + byteBuffer.WriterIndex, byteBuffer.WritableBytes), CancellationToken.None).ConfigureAwait(false);
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                throw new ProtocolViolationException("Mqtt over WS message cannot be in text");
            }

            // Check if client closed WebSocket
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return -1;
            }

            byteBuffer.SetWriterIndex(byteBuffer.WriterIndex + receiveResult.Count);
            return receiveResult.Count;
        }

        void CancelPendingWrite()
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

        async Task HandleCloseAsync()
        {
            try
            {
                await CloseAsync().ConfigureAwait(false);
            }
            catch (Exception) //when (!Fx.IsFatal(ex))
            {
                Abort();
            }
        }

        void Abort()
        {
            _webSocket.Abort();
            _webSocket.Dispose();
            _writeCancellationTokenSource.Dispose();
        }
    }

    public class ServerWebSocketChannelConfig : IChannelConfiguration
    {
        public TimeSpan ConnectTimeout { get; set; }

        public int WriteSpinCount { get; set; }

        public IByteBufferAllocator Allocator { get; set; }

        public IRecvByteBufAllocator RecvByteBufAllocator { get; set; }

        public bool AutoRead { get; set; }

        public int WriteBufferHighWaterMark { get; set; }

        public int WriteBufferLowWaterMark { get; set; }

        public IMessageSizeEstimator MessageSizeEstimator { get; set; }

        public T GetOption<T>(ChannelOption<T> option)
        {
            Contract.Requires(option != null);

            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                return (T)(object)ConnectTimeout; // no boxing will happen, compiler optimizes away such casts
            }
            if (ChannelOption.WriteSpinCount.Equals(option))
            {
                return (T)(object)WriteSpinCount;
            }
            if (ChannelOption.Allocator.Equals(option))
            {
                return (T)Allocator;
            }
            if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                return (T)RecvByteBufAllocator;
            }
            if (ChannelOption.AutoRead.Equals(option))
            {
                return (T)(object)AutoRead;
            }
            if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                return (T)(object)WriteBufferHighWaterMark;
            }
            if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                return (T)(object)WriteBufferLowWaterMark;
            }
            if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                return (T)MessageSizeEstimator;
            }
            return default(T);
        }

        public bool SetOption(ChannelOption option, object value) => option.Set(this, value);

        public bool SetOption<T>(ChannelOption<T> option, T value)
        {
            // Validate(option, value);

            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                ConnectTimeout = (TimeSpan)(object)value;
            }
            else if (ChannelOption.WriteSpinCount.Equals(option))
            {
                WriteSpinCount = (int)(object)value;
            }
            else if (ChannelOption.Allocator.Equals(option))
            {
                Allocator = (IByteBufferAllocator)value;
            }
            else if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                RecvByteBufAllocator = (IRecvByteBufAllocator)value;
            }
            else if (ChannelOption.AutoRead.Equals(option))
            {
                AutoRead = (bool)(object)value;
            }
            else if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                WriteBufferHighWaterMark = (int)(object)value;
            }
            else if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                WriteBufferLowWaterMark = (int)(object)value;
            }
            else if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                MessageSizeEstimator = (IMessageSizeEstimator)value;
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
