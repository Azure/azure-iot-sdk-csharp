// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Client websocket channel operations.
    /// </summary>
    internal class ClientWebSocketChannel : AbstractChannel, IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _writeCancellationTokenSource;
        private bool _isActive;
        private bool _isReadPending;
        private bool _isWriteInProgress;

        /// <summary>
        /// Creates a new instance of the client websocket channel.
        /// </summary>
        /// <param name="parent">The parent of this channel. Pass null if there's no parent.</param>
        /// <param name="webSocket">The client websocket. Provides a client for connecting to WebSocket services.</param>
        public ClientWebSocketChannel(IChannel parent, ClientWebSocket webSocket)
            : base(parent)
        {
            _webSocket = webSocket;
            _isActive = true;
            Metadata = new ChannelMetadata(false, 16);
            Configuration = new ClientWebSocketChannelConfig();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Websocket channel configurations.
        /// </summary>
        public override IChannelConfiguration Configuration { get; }

        /// <summary>
        /// Whether or not the websocket channel is open and active.
        /// </summary>
        public override bool Open => _isActive && _webSocket?.State == WebSocketState.Open;

        /// <summary>
        /// Whether or not the websocket channel is active.
        /// </summary>
        public override bool Active => _isActive;

        /// <summary>
        /// Represents the metadata properties of a DotNetty.Transport.Channels.IChannel implementation.
        /// </summary>
        public override ChannelMetadata Metadata { get; }

        /// <summary>
        /// Identifies a local network address
        /// </summary>
        protected override EndPoint LocalAddressInternal { get; }

        /// <summary>
        /// Identifies a remote network address
        /// </summary>
        protected override EndPoint RemoteAddressInternal { get; }

        /// <summary>
        /// Creates a new UnsafeChannel off of this websocket channel.
        /// </summary>
        protected override IChannelUnsafe NewUnsafe() => new WebSocketChannelUnsafe(this);

        /// <summary>
        /// Checks whether a given EventLoop is compatible with the Websocket channel.
        /// </summary>
        /// <param name="eventLoop">EventLoop to check compatibility.</param>
        protected override bool IsCompatible(IEventLoop eventLoop) => true;

        /// <summary>
        /// Set channel options fluently.
        /// </summary>
        /// <typeparam name="T">Generic Option type.</typeparam>
        /// <param name="option">Channel option.</param>
        /// <param name="value">Option value.</param>
        /// <returns>ClientWebSocketChannel object itself.</returns>
        public ClientWebSocketChannel Option<T>(ChannelOption<T> option, T value)
        {
            Contract.Requires(option != null);

            Configuration.SetOption(option, value);
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _webSocket?.Dispose();

            _writeCancellationTokenSource?.Dispose();
            _writeCancellationTokenSource = null;
        }

        private class WebSocketChannelUnsafe : AbstractUnsafe
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
                if (((ClientWebSocketChannel)channel)._isWriteInProgress)
                {
                    return;
                }

                base.Flush0();
            }
        }

        /// <summary>
        /// Binds the client websocket channel to the EndPoint.
        /// </summary>
        /// <param name="endpointAddress">The endpoint address to bind to.</param>
        protected override void DoBind(EndPoint endpointAddress)
        {
            throw new NotSupportedException("ClientWebSocketChannel does not support DoBind()");
        }

        /// <summary>
        /// Disconnects this channel from its remote peer.
        /// </summary>
        protected override void DoDisconnect()
        {
            throw new NotSupportedException("ClientWebSocketChannel does not support DoDisconnect()");
        }

        /// <summary>
        /// Closes the websocket channel.
        /// </summary>
        protected override async void DoClose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DoClose));

            try
            {
                WebSocketState webSocketState = _webSocket.State;
                if (webSocketState != WebSocketState.Closed && webSocketState != WebSocketState.Aborted)
                {
                    // Cancel any pending write
                    CancelPendingWrite();
                    _isActive = false;

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                Abort();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DoClose));
            }
        }

        /// <summary>
        /// ScheduleAsync a read operation.
        /// </summary>
        protected override async void DoBeginRead()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DoBeginRead));

            try
            {
                IByteBuffer byteBuffer = null;
                IRecvByteBufAllocatorHandle allocHandle = null;
                bool close = false;
                try
                {
                    if (!Open || _isReadPending)
                    {
                        return;
                    }

                    _isReadPending = true;
                    IByteBufferAllocator allocator = Configuration.Allocator;
                    allocHandle = Configuration.RecvByteBufAllocator.NewHandle();
                    allocHandle.Reset(Configuration);
                    do
                    {
                        byteBuffer = allocHandle.Allocate(allocator);
                        allocHandle.LastBytesRead = await DoReadBytesAsync(byteBuffer).ConfigureAwait(false);
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
                    } while (allocHandle.ContinueReading());

                    allocHandle.ReadComplete();
                    _isReadPending = false;
                    Pipeline.FireChannelReadComplete();
                }
                catch (Exception ex) when (!Fx.IsFatal(ex))
                {
                    // Since this method returns void, all exceptions must be handled here.
                    byteBuffer?.Release();
                    allocHandle?.ReadComplete();
                    _isReadPending = false;
                    Pipeline.FireChannelReadComplete();
                    Pipeline.FireExceptionCaught(ex);
                    close = true;
                }

                if (close && Active)
                {
                    await HandleCloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DoBeginRead));
            }
        }

        /// <summary>
        /// Flush the content of the given buffer to the remote peer.
        /// </summary>
        /// <param name="channelOutboundBuffer">The buffer to write to the remote peer.</param>
        protected override async void DoWrite(ChannelOutboundBuffer channelOutboundBuffer)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DoWrite));

            if (channelOutboundBuffer == null)
            {
                throw new ArgumentNullException(nameof(channelOutboundBuffer), "The channel outbound buffer cannot be null.");
            }

            try
            {
                // The ChannelOutboundBuffer might have more than one message per MQTT packet that needs to be written to the websocket as a single frame.
                // One example of this is a PUBLISH packet, which encodes the payload and topic information as separate messages.
                // In order to reduce the number of frames sent to the websocket, we will consolidate the individual messages per MQTT packet into a single byte buffer.
                IByteBufferAllocator allocator = Configuration.Allocator;

                // The parameter "direct" is used to indicate if operations carried out in the CompositeByteBuffer should be treated as "unsafe".
                var compositeByteBuffer = new CompositeByteBuffer(allocator, direct: false, maxNumComponents: int.MaxValue);

                var bytesToBeWritten = new ArraySegment<byte>();
                _isWriteInProgress = true;

                while (true)
                {
                    object currentMessage = channelOutboundBuffer.Current;

                    // Once there are no more messages pending in ChannelOutboundBuffer, the "Current" property is returned as "null".
                    // This indicates that all pending messages have been dequeued from the ChannelOutboundBuffer and are ready to be written to the websocket.
                    if (currentMessage == null)
                    {
                        // This indicates that the ChannelOutboundBuffer had readable bytes and they have been added to the CompositeByteBuffer.
                        if (compositeByteBuffer.NumComponents > 0)
                        {
                            // All messages have been added to the CompositeByteBuffer and are now ready to be written to the socket.
                            bytesToBeWritten = compositeByteBuffer.GetIoBuffer();
                        }
                        break;
                    }

                    var byteBuffer = currentMessage as IByteBuffer;
                    Fx.AssertAndThrow(byteBuffer != null, "channelOutBoundBuffer contents must be of type IByteBuffer");

                    // If the byte buffer has readable bytes then add them to the CompositeByteBuffer.
                    if (byteBuffer.ReadableBytes != 0)
                    {
                        // There are two operations carried out while adding a byte buffer component to a CompositeByteBuffer:
                        // - Increase WriterIndex of the CompositeByteBuffer
                        //      - increases the count of readable bytes added to the CompositeByteBuffer.
                        // - Call the method Retain() on the byte buffer being added
                        //      - The property ReferenceCount of a byte buffer implementation maintains a counter of the no of messages available for dequeuing.
                        //        A ReferenceCount of 0 indicates that all messages have been flushed and the buffer can be deallocated.
                        //        By calling the method Retain() on each byte buffer component added to the CompositeByteBuffer,
                        //        we increase the ReferenceCount by 1 and mark them as ready for dequeuing.
                        compositeByteBuffer
                            .AddComponent(
                                increaseWriterIndex: true,
                                buffer: (IByteBuffer)byteBuffer.Retain());
                    }

                    // Once the readable bytes are added to the CompositeByteBuffer they can be removed from the ChannelOutboundBuffer
                    // and the next message, if any, can be processed.
                    channelOutboundBuffer.Remove();
                }

                if (bytesToBeWritten.Count > 0)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Writing bytes of size {bytesToBeWritten.Count} to the websocket", nameof(DoWrite));

                    await _webSocket.SendAsync(bytesToBeWritten, WebSocketMessageType.Binary, true, _writeCancellationTokenSource.Token).ConfigureAwait(false);
                }

                _isWriteInProgress = false;
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                // Since this method returns void, all exceptions must be handled here.

                _isWriteInProgress = false;
                Pipeline.FireExceptionCaught(ex);
                await HandleCloseAsync().ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DoWrite));
            }
        }

        private async Task<int> DoReadBytesAsync(IByteBuffer byteBuffer)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(DoReadBytesAsync));

            try
            {
                WebSocketReceiveResult receiveResult = await _webSocket
                    .ReceiveAsync(new ArraySegment<byte>(byteBuffer.Array, byteBuffer.ArrayOffset + byteBuffer.WriterIndex, byteBuffer.WritableBytes), CancellationToken.None)
                    .ConfigureAwait(false);
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
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(DoReadBytesAsync));
            }
        }

        private void CancelPendingWrite()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CancelPendingWrite));

            try
            {
                _writeCancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore this error
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CancelPendingWrite));
            }
        }

        private async Task HandleCloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(HandleCloseAsync));

            try
            {
                await CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                Abort();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(HandleCloseAsync));
            }
        }

        private void Abort()
        {
            _webSocket?.Abort();
        }
    }
}
