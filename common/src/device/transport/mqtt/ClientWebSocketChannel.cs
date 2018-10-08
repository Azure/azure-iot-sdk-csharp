// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using System.Diagnostics;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - WS is owned by the caller.
    internal class ClientWebSocketChannel : AbstractChannel
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly ClientWebSocket _webSocket;
        private readonly CancellationTokenSource _writeCancellationTokenSource;
        private bool _active;

        internal bool ReadPending { get; set; }

        internal bool WriteInProgress { get; set; }

        public ClientWebSocketChannel(IChannel parent, ClientWebSocket webSocket)
            : base(parent)
        {
            _webSocket = webSocket;
            _active = true;
            Metadata = new ChannelMetadata(false, 16);
            Configuration = new ClientWebSocketChannelConfig();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        public override IChannelConfiguration Configuration { get; }

        public override bool Open => (_webSocket.State == WebSocketState.Open && Active);

        public override bool Active => _active;

        public override ChannelMetadata Metadata { get; }

        public ClientWebSocketChannel Option<T>(ChannelOption<T> option, T value)
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
                : base(channel)
            {
            }

            public override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
            {
                throw new NotSupportedException("ClientWebSocketChannel does not support ConnectAsync()");
            }

            protected override void Flush0()
            {
                // Flush immediately only when there's no pending flush.
                // If there's a pending flush operation, event loop will call FinishWrite() later,
                // and thus there's no need to call it now.
                if (((ClientWebSocketChannel)channel).WriteInProgress)
                {
                    return;
                }

                base.Flush0();
            }
        }

        protected override bool IsCompatible(IEventLoop eventLoop) => true;

        protected override void DoBind(EndPoint localAddress)
        {
            throw new NotSupportedException("ClientWebSocketChannel does not support DoBind()");
        }

        protected override void DoDisconnect()
        {
            throw new NotSupportedException("ClientWebSocketChannel does not support DoDisconnect()");
        }

        protected override void DoClose()
        {
            WebSocketState webSocketState = _webSocket.State;
            if (webSocketState != WebSocketState.Closed && webSocketState != WebSocketState.Aborted)
            {
                // Cancel any pending write
                CancelPendingWrite();
                _active = false;

                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                        {
                            await _webSocket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                string.Empty,
                                cancellationTokenSource.Token);
                        }

                        _webSocket.Dispose();
                    }
                    catch (Exception)
                    {
                        Abort();
                    }
                });
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
                    allocHandle.LastBytesRead = await DoReadBytes(byteBuffer);
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
            catch (Exception e)
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
                    await HandleCloseAsync();
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
                    Debug.Assert(byteBuffer != null, "channelOutBoundBuffer contents must be of type IByteBuffer");

                    if (byteBuffer.ReadableBytes == 0)
                    {
                        channelOutboundBuffer.Remove();
                        continue;
                    }

                    await _webSocket.SendAsync(
                        byteBuffer.GetIoBuffer(),
                        WebSocketMessageType.Binary,
                        true,
                        _writeCancellationTokenSource.Token);

                    channelOutboundBuffer.Remove();
                }

                WriteInProgress = false;
            }
            catch (Exception e)
            {
                // Since this method returns void, all exceptions must be handled here.

                WriteInProgress = false;
                Pipeline.FireExceptionCaught(e);
                await HandleCloseAsync();
            }
        }

        private async Task<int> DoReadBytes(IByteBuffer byteBuffer)
        {
            WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(byteBuffer.Array, byteBuffer.ArrayOffset + byteBuffer.WriterIndex, byteBuffer.WritableBytes),
                CancellationToken.None);

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

        private async Task HandleCloseAsync()
        {
            try
            {
                await CloseAsync();
            }
            catch (Exception)
            {
                Abort();
            }
        }

        private void Abort()
        {
            _webSocket.Abort();
            _webSocket.Dispose();
            _writeCancellationTokenSource.Dispose();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            return !(left == right);
        }

        public static bool operator <(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ClientWebSocketChannel left, ClientWebSocketChannel right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

#pragma warning disable CA1822
        public int CompareTo(ClientWebSocketChannel other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            throw new NotImplementedException();
        }
#pragma warning restore CA1822
    }
}
