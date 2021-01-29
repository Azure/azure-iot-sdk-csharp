// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Client Websocket channel configurations
    /// </summary>
    public class ClientWebSocketChannelConfig : IChannelConfiguration
    {
        /// <summary>
        /// Gets channel options from the configuration.
        /// </summary>
        /// <typeparam name="T">Generic Type of the option to get.</typeparam>
        /// <param name="option">The option to retrieve.</param>
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
            return default;
        }

        /// <summary>
        /// Set a channel option.
        /// </summary>
        /// <param name="option">The option to set.</param>
        /// <param name="value">The option value.</param>
        public bool SetOption(ChannelOption option, object value)
        {
            return option == null
                ? throw new ArgumentNullException(nameof(option), "The Channel Option cannot be null.")
                : option.Set(this, value);
        }

        /// <summary>
        /// Set a channel option.
        /// </summary>
        /// <param name="option">The option to set.</param>
        /// <param name="value">The option value.</param>
        public bool SetOption<T>(ChannelOption<T> option, T value)
        {
            // this.Validate(option, value);

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

        /// <summary>
        /// Channel connection timeout.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Write spin count.
        /// </summary>
        public int WriteSpinCount { get; set; }

        /// <summary>
        /// Thread-safe interface for allocating IByteBuffer
        /// </summary>
        public IByteBufferAllocator Allocator { get; set; }

        /// <summary>
        /// Allocates a new receive buffer whose capacity is probably large enough to read
        /// all inbound data and small enough not to waste its space.</summary>
        public IRecvByteBufAllocator RecvByteBufAllocator { get; set; }

        /// <summary>
        /// Whether or not auto-read is enabled.
        /// </summary>
        public bool AutoRead { get; set; }

        /// <summary>
        /// Write buffer high water mark.
        /// </summary>
        public int WriteBufferHighWaterMark { get; set; }

        /// <summary>
        /// Write buffer low water mark
        /// </summary>
        public int WriteBufferLowWaterMark { get; set; }

        /// <summary>
        /// Calculates the size of the given message.
        /// </summary>
        public IMessageSizeEstimator MessageSizeEstimator { get; set; }
    }
}
